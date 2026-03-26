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
        [SerializeField] private Collider targetCollider;
        [SerializeField] private Renderer[] targetRenderers;

        private float _currentHitPoints;
        private Vector3 _startLocalPosition;
        private bool _hidden;
        private float _respawnAt;

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
                SetHidden(false);
            }
        }

        public void ApplyHit(float damage)
        {
            if (_hidden)
            {
                return;
            }

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
