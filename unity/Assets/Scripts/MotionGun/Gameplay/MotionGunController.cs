using System;
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
        [SerializeField] private float maxPacketAgeSeconds = 0.35f;
        [SerializeField] private List<WeaponConfig> weapons = new List<WeaponConfig>();

        private readonly Dictionary<int, WeaponConfig> _weaponsBySlot = new Dictionary<int, WeaponConfig>();
        private readonly Dictionary<int, int> _ammoBySlot = new Dictionary<int, int>();
        private GesturePacket _latestPacket = new GesturePacket();
        private WeaponConfig _currentWeapon;
        private Coroutine _tracerCoroutine;
        private WeaponState _state = WeaponState.TrackingLost;
        private float _reloadCompleteAt;
        private float _lastShotTime = float.NegativeInfinity;
        private bool _wasTrackingReady;
        private bool _combatActive;
        private bool _consumeCurrentFirePulse;
        private int _shotsFired;
        private int _shotsHit;
        private int _score;
        private int _hudCurrentWave;
        private int _hudTotalWaves;
        private int _hudRemainingTargets;
        private float _hudTimeRemainingSeconds;
        private string _hudSessionBanner = "FIRE TO START";
        private string _eventText = "START PYTHON SENDER";
        private float _eventExpiresAt;

        public event Action FirePulse;

        public bool TrackingReady => IsTrackingReady();

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

            if (gestureClient.HasPacket)
            {
                _latestPacket = gestureClient.LatestPacket;
            }

            bool trackingReady = IsTrackingReady();
            _consumeCurrentFirePulse = false;

            UpdateTrackingState(trackingReady);
            CompleteReloadIfReady();
            UpdateAim();

            if (trackingReady)
            {
                if (_latestPacket.fire)
                {
                    if (FirePulse != null)
                    {
                        FirePulse();
                    }
                }

                if (_combatActive)
                {
                    if (_latestPacket.weapon_slot > 0)
                    {
                        SwitchWeapon(_latestPacket.weapon_slot, false);
                    }

                    if (_latestPacket.reload)
                    {
                        StartReload();
                    }

                    if (_latestPacket.fire && !_consumeCurrentFirePulse)
                    {
                        TryFire();
                    }
                }
            }

            UpdateHud();
        }

        public void SetCombatActive(bool combatActive)
        {
            _combatActive = combatActive;
        }

        public void ConsumeCurrentFirePulse()
        {
            _consumeCurrentFirePulse = true;
        }

        public void ResetSession()
        {
            BuildWeaponTable();
            _shotsFired = 0;
            _shotsHit = 0;
            _score = 0;
            _lastShotTime = float.NegativeInfinity;
            _reloadCompleteAt = 0f;
            _state = IsTrackingReady() ? WeaponState.Idle : WeaponState.TrackingLost;
            _eventText = string.Empty;
            _eventExpiresAt = 0f;

            if (weapons.Count > 0)
            {
                SwitchWeapon(weapons[0].SlotId, true);
            }

            if (_tracerCoroutine != null)
            {
                StopCoroutine(_tracerCoroutine);
                _tracerCoroutine = null;
            }

            if (tracer != null)
            {
                tracer.enabled = false;
            }
        }

        public void ConfigureSessionHud(
            int currentWave,
            int totalWaves,
            int remainingTargets,
            float timeRemainingSeconds,
            string sessionBanner
        )
        {
            _hudCurrentWave = currentWave;
            _hudTotalWaves = totalWaves;
            _hudRemainingTargets = remainingTargets;
            _hudTimeRemainingSeconds = timeRemainingSeconds;
            _hudSessionBanner = sessionBanner ?? string.Empty;
        }

        public void AddScore(int amount)
        {
            _score += Mathf.Max(0, amount);
            SetTransientEvent("TARGET DOWN", 0.6f);
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

        private void UpdateTrackingState(bool trackingReady)
        {
            if (!trackingReady)
            {
                if (_wasTrackingReady)
                {
                    SetTransientEvent("TRACK LOST", 0.9f);
                }

                if (_state != WeaponState.Reloading)
                {
                    _state = WeaponState.TrackingLost;
                }

                _wasTrackingReady = false;
                return;
            }

            if (_state == WeaponState.TrackingLost)
            {
                _state = WeaponState.Idle;
            }

            _wasTrackingReady = true;
        }

        private void CompleteReloadIfReady()
        {
            if (!_combatActive || _state != WeaponState.Reloading || Time.time < _reloadCompleteAt)
            {
                return;
            }

            _ammoBySlot[_currentWeapon.SlotId] = _currentWeapon.MagazineSize;
            _state = IsTrackingReady() ? WeaponState.Idle : WeaponState.TrackingLost;
            SetTransientEvent("RELOADED", 0.8f);
        }

        private bool HasFreshSignal()
        {
            return gestureClient != null && gestureClient.HasFreshPacket(maxPacketAgeSeconds);
        }

        private bool IsTrackingReady()
        {
            return HasFreshSignal()
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

            if (!force)
            {
                SetTransientEvent("WEAPON " + weapon.DisplayName.ToUpperInvariant(), 0.8f);
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
            SetTransientEvent("RELOAD", 0.8f);
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
                SetTransientEvent("MAG EMPTY", 0.7f);
                StartReload();
                return;
            }

            _lastShotTime = Time.time;
            _shotsFired += 1;
            _ammoBySlot[_currentWeapon.SlotId] = ammo - 1;

            Ray ray = BuildAimRay();
            Vector3 tracerEnd = ray.origin + (ray.direction * maxRange);
            bool targetHit = false;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                tracerEnd = hit.point;
                targetHit = ApplyDamage(hit, _currentWeapon.Damage);
            }

            if (targetHit)
            {
                _shotsHit += 1;
                SetTransientEvent("TARGET HIT", 0.5f);
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
                SetTransientEvent("MAG EMPTY", 0.7f);
                StartReload();
            }
        }

        private bool ApplyDamage(RaycastHit hit, float damage)
        {
            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                IMotionGunTarget target = behaviour as IMotionGunTarget;
                if (target != null)
                {
                    target.ApplyHit(damage);
                    return true;
                }
            }

            return false;
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

        private void SetTransientEvent(string eventText, float duration)
        {
            _eventText = eventText;
            _eventExpiresAt = Time.time + duration;
        }

        private string GetEventText()
        {
            if (!string.IsNullOrEmpty(_eventText) && Time.time < _eventExpiresAt)
            {
                return _eventText;
            }

            if (!HasFreshSignal())
            {
                return "START PYTHON SENDER";
            }

            if (!_latestPacket.primary_hand_detected)
            {
                return "FORM GUN POSE";
            }

            if (!_combatActive)
            {
                return "FIRE TO START";
            }

            if (!_latestPacket.secondary_hand_detected)
            {
                return "SHOW OFFHAND";
            }

            if (_state == WeaponState.Reloading)
            {
                return "RELOAD IN PROGRESS";
            }

            return "LOCKED";
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
            else if (!HasFreshSignal())
            {
                status = "NO SIGNAL";
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
                _latestPacket.tracking_confidence,
                _score,
                _shotsFired,
                _shotsHit,
                GetEventText(),
                _hudCurrentWave,
                _hudTotalWaves,
                _hudRemainingTargets,
                _hudTimeRemainingSeconds,
                _hudSessionBanner
            );
        }
    }
}

