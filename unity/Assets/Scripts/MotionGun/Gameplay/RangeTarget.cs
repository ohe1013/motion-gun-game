using System;
using System.Collections;
using UnityEngine;

namespace MotionGun.Gameplay
{
    public class RangeTarget : MonoBehaviour, IMotionGunTarget
    {
        [SerializeField] private float hitPoints = 1f;
        [SerializeField] private Vector3 travelAxis = Vector3.right;
        [SerializeField] private float travelDistance = 1.6f;
        [SerializeField] private float travelSpeed = 1f;
        [SerializeField] private Color targetColor = new Color(0.8f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.95f, 0.3f, 1f);
        [SerializeField] private float hitFlashDuration = 0.08f;
        [SerializeField] private Collider targetCollider;
        [SerializeField] private Renderer[] targetRenderers;

        private float _baseTravelDistance;
        private float _baseTravelSpeed;
        private float _currentHitPoints;
        private Vector3 _startLocalPosition;
        private bool _hidden;
        private bool _activeForWave;
        private bool _cleared;
        private float _runtimeTravelDistance;
        private float _runtimeTravelSpeed;
        private Coroutine _hitFlashCoroutine;

        public event Action<RangeTarget> Cleared;

        private void Awake()
        {
            if (targetCollider == null)
            {
                targetCollider = GetComponent<Collider>();
            }

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            }

            _startLocalPosition = transform.localPosition;
            _baseTravelDistance = travelDistance;
            _baseTravelSpeed = travelSpeed;
            _currentHitPoints = hitPoints;
            ApplyTargetColor(targetColor);
            SetHidden(false);
        }

        private void Update()
        {
            if (!_activeForWave)
            {
                return;
            }

            if (_runtimeTravelDistance > 0f && travelAxis.sqrMagnitude > 0f)
            {
                Vector3 offset = travelAxis.normalized * Mathf.Sin(Time.time * _runtimeTravelSpeed) * _runtimeTravelDistance;
                transform.localPosition = _startLocalPosition + offset;
            }
        }

        public void ConfigureForWave(float waveHitPoints, bool moving, float speedMultiplier)
        {
            hitPoints = Mathf.Max(0.1f, waveHitPoints);
            _currentHitPoints = hitPoints;
            _runtimeTravelDistance = moving ? _baseTravelDistance : 0f;
            _runtimeTravelSpeed = moving ? (_baseTravelSpeed * Mathf.Max(0.1f, speedMultiplier)) : 0f;
            _activeForWave = true;
            _cleared = false;
            transform.localPosition = _startLocalPosition;
            ApplyTargetColor(targetColor);
            SetHidden(false);
        }

        public void DeactivateForWave()
        {
            if (_hitFlashCoroutine != null)
            {
                StopCoroutine(_hitFlashCoroutine);
                _hitFlashCoroutine = null;
            }

            _activeForWave = false;
            _cleared = false;
            _currentHitPoints = hitPoints;
            transform.localPosition = _startLocalPosition;
            ApplyTargetColor(targetColor);
            SetHidden(true);
        }

        public void ApplyHit(float damage)
        {
            if (_hidden || !_activeForWave || _cleared)
            {
                return;
            }

            if (_hitFlashCoroutine != null)
            {
                StopCoroutine(_hitFlashCoroutine);
            }
            _hitFlashCoroutine = StartCoroutine(FlashHit());

            _currentHitPoints -= damage;
            if (_currentHitPoints > 0f)
            {
                return;
            }

            _cleared = true;
            _activeForWave = false;
            SetHidden(true);
            if (Cleared != null)
            {
                Cleared(this);
            }
        }

        private IEnumerator FlashHit()
        {
            ApplyTargetColor(hitFlashColor);
            yield return new WaitForSeconds(hitFlashDuration);
            ApplyTargetColor(targetColor);
            _hitFlashCoroutine = null;
        }

        private void ApplyTargetColor(Color color)
        {
            if (targetRenderers == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer != null && targetRenderer.material.HasProperty("_Color"))
                {
                    targetRenderer.material.color = color;
                }
            }
        }

        private void SetHidden(bool hidden)
        {
            _hidden = hidden;

            if (targetCollider != null)
            {
                targetCollider.enabled = !hidden;
            }

            if (targetRenderers == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = !hidden;
                }
            }
        }
    }
}
