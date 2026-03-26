using System.Collections;
using UnityEngine;

namespace MotionGun.Gameplay
{
    public class RangeTarget : MonoBehaviour, IMotionGunTarget
    {
        [SerializeField] private float hitPoints = 1f;
        [SerializeField] private bool respawnAfterHit = true;
        [SerializeField] private float respawnDelay = 1.25f;
        [SerializeField] private Vector3 travelAxis = Vector3.right;
        [SerializeField] private float travelDistance = 0f;
        [SerializeField] private float travelSpeed = 1f;
        [SerializeField] private Color targetColor = new Color(0.8f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.95f, 0.3f, 1f);
        [SerializeField] private float hitFlashDuration = 0.08f;
        [SerializeField] private Collider targetCollider;
        [SerializeField] private Renderer[] targetRenderers;

        private float _currentHitPoints;
        private Vector3 _startLocalPosition;
        private bool _hidden;
        private float _respawnAt;
        private Coroutine _hitFlashCoroutine;

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
            _currentHitPoints = hitPoints;
            ApplyTargetColor(targetColor);
            SetHidden(false);
        }

        private void Update()
        {
            if (travelDistance > 0f && travelAxis.sqrMagnitude > 0f)
            {
                Vector3 offset = travelAxis.normalized * Mathf.Sin(Time.time * travelSpeed) * travelDistance;
                transform.localPosition = _startLocalPosition + offset;
            }

            if (_hidden && respawnAfterHit && Time.time >= _respawnAt)
            {
                _currentHitPoints = hitPoints;
                ApplyTargetColor(targetColor);
                SetHidden(false);
            }
        }

        public void ApplyHit(float damage)
        {
            if (_hidden)
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

            if (respawnAfterHit)
            {
                _respawnAt = Time.time + respawnDelay;
                SetHidden(true);
                return;
            }

            Destroy(gameObject);
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
