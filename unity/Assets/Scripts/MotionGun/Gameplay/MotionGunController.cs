using System.Collections;
using System.Collections.Generic;
using MotionGun.Runtime;
using MotionGun.UI;
using UnityEngine;

namespace MotionGun.Gameplay
{
    public class MotionGunController : MonoBehaviour
    {
        private enum WeaponState
        {
            Idle,
            Reloading,
            TrackingLost,
        }

        [SerializeField] private UdpGestureClient gestureClient;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private RangeHudController hud;
        [SerializeField] private AimReticleController reticle;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float maxRange = 250f;
        [SerializeField] private float minTrackingConfidence = 0.45f;
        [SerializeField] private List<WeaponConfig> weapons = new List<WeaponConfig>();

        private readonly Dictionary<int, WeaponConfig> _weaponsBySlot = new Dictionary<int, WeaponConfig>();
        private readonly Dictionary<int, int> _ammoBySlot = new Dictionary<int, int>();
        private GesturePacket _latestPacket = new GesturePacket();
        private WeaponConfig _currentWeapon;
        private Coroutine _tracerCoroutine;
        private WeaponState _state = WeaponState.TrackingLost;
        private float _reloadCompleteAt;
        private float _lastShotTime = float.NegativeInfinity;

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            BuildWeaponTable();
            if (weapons.Count > 0)
            {
                SwitchWeapon(weapons[0].SlotId, true);
            }

            if (tracer != null)
            {
                tracer.enabled = false;
            }
        }

        private void Update()
        {
            if (gestureClient == null || aimCamera == null || _currentWeapon == null)
            {
                return;
            }

            _latestPacket = gestureClient.LatestPacket;
            UpdateTrackingState();
            CompleteReloadIfReady();
            UpdateAim();

            if (IsTrackingReady())
            {
                if (_latestPacket.weapon_slot > 0)
                {
                    SwitchWeapon(_latestPacket.weapon_slot, false);
                }

                if (_latestPacket.reload)
                {
                    StartReload();
                }

                if (_latestPacket.fire)
                {
                    TryFire();
                }
            }

            UpdateHud();
        }

        private void BuildWeaponTable()
        {
            _weaponsBySlot.Clear();
            _ammoBySlot.Clear();

            foreach (WeaponConfig weapon in weapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                _weaponsBySlot[weapon.SlotId] = weapon;
                if (!_ammoBySlot.ContainsKey(weapon.SlotId))
                {
                    _ammoBySlot.Add(weapon.SlotId, weapon.MagazineSize);
                }
            }
        }

        private void UpdateTrackingState()
        {
            bool trackingReady = IsTrackingReady();
            if (!trackingReady)
            {
                if (_state != WeaponState.Reloading)
                {
                    _state = WeaponState.TrackingLost;
                }
                return;
            }

            if (_state == WeaponState.TrackingLost)
            {
                _state = WeaponState.Idle;
            }
        }

        private void CompleteReloadIfReady()
        {
            if (_state != WeaponState.Reloading || Time.time < _reloadCompleteAt)
            {
                return;
            }

            _ammoBySlot[_currentWeapon.SlotId] = _currentWeapon.MagazineSize;
            _state = IsTrackingReady() ? WeaponState.Idle : WeaponState.TrackingLost;
        }

        private bool IsTrackingReady()
        {
            return gestureClient != null
                && gestureClient.HasPacket
                && _latestPacket.primary_hand_detected
                && _latestPacket.tracking_confidence >= minTrackingConfidence;
        }

        private void UpdateAim()
        {
            Vector2 aim = new Vector2(
                Mathf.Clamp01(_latestPacket.aim_x),
                Mathf.Clamp01(_latestPacket.aim_y)
            );

            if (reticle != null)
            {
                reticle.SetNormalizedAim(aim);
            }

            if (weaponPivot == null)
            {
                return;
            }

            Ray ray = BuildAimRay();
            Vector3 targetPoint = ray.origin + (ray.direction * maxRange);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;
            }

            Vector3 lookDirection = targetPoint - weaponPivot.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                weaponPivot.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }
        }

        private Ray BuildAimRay()
        {
            return aimCamera.ViewportPointToRay(
                new Vector3(
                    Mathf.Clamp01(_latestPacket.aim_x),
                    Mathf.Clamp01(1f - _latestPacket.aim_y),
                    0f
                )
            );
        }

        private void SwitchWeapon(int slotId, bool force)
        {
            WeaponConfig weapon;
            if (!_weaponsBySlot.TryGetValue(slotId, out weapon))
            {
                return;
            }

            if (!force && _currentWeapon != null && _currentWeapon.SlotId == slotId)
            {
                return;
            }

            _currentWeapon = weapon;
            if (!_ammoBySlot.ContainsKey(slotId))
            {
                _ammoBySlot[slotId] = weapon.MagazineSize;
            }

            if (_state != WeaponState.TrackingLost)
            {
                _state = WeaponState.Idle;
            }
        }

        private void StartReload()
        {
            if (_currentWeapon == null)
            {
                return;
            }

            if (_state == WeaponState.Reloading)
            {
                return;
            }

            if (_ammoBySlot[_currentWeapon.SlotId] >= _currentWeapon.MagazineSize)
            {
                return;
            }

            _state = WeaponState.Reloading;
            _reloadCompleteAt = Time.time + _currentWeapon.ReloadDuration;
        }

        private void TryFire()
        {
            if (_state != WeaponState.Idle || _currentWeapon == null)
            {
                return;
            }

            if (Time.time - _lastShotTime < _currentWeapon.FireInterval)
            {
                return;
            }

            int ammo = _ammoBySlot[_currentWeapon.SlotId];
            if (ammo <= 0)
            {
                StartReload();
                return;
            }

            _lastShotTime = Time.time;
            _ammoBySlot[_currentWeapon.SlotId] = ammo - 1;

            Ray ray = BuildAimRay();
            Vector3 tracerEnd = ray.origin + (ray.direction * maxRange);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                tracerEnd = hit.point;
                ApplyDamage(hit, _currentWeapon.Damage);
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            if (tracer != null)
            {
                if (_tracerCoroutine != null)
                {
                    StopCoroutine(_tracerCoroutine);
                }
                _tracerCoroutine = StartCoroutine(FlashTracer(ray.origin, tracerEnd));
            }

            if (_ammoBySlot[_currentWeapon.SlotId] <= 0)
            {
                StartReload();
            }
        }

        private void ApplyDamage(RaycastHit hit, float damage)
        {
            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                IMotionGunTarget target = behaviour as IMotionGunTarget;
                if (target != null)
                {
                    target.ApplyHit(damage);
                    break;
                }
            }
        }

        private IEnumerator FlashTracer(Vector3 start, Vector3 end)
        {
            tracer.positionCount = 2;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            yield return new WaitForSeconds(0.05f);
            tracer.enabled = false;
            _tracerCoroutine = null;
        }

        private void UpdateHud()
        {
            if (hud == null || _currentWeapon == null)
            {
                return;
            }

            string status;
            if (_state == WeaponState.Reloading)
            {
                status = "RELOADING";
            }
            else if (_state == WeaponState.TrackingLost)
            {
                status = "TRACK LOST";
            }
            else
            {
                status = "READY";
            }

            hud.RenderSnapshot(
                _currentWeapon.DisplayName,
                _ammoBySlot[_currentWeapon.SlotId],
                _currentWeapon.MagazineSize,
                status,
                _latestPacket.tracking_confidence
            );
        }
    }
}
