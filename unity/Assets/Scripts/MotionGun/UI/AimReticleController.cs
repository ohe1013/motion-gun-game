using UnityEngine;

namespace MotionGun.UI
{
    public class AimReticleController : MonoBehaviour
    {
        [SerializeField] private RectTransform reticle;
        [SerializeField] private RectTransform canvasRect;

        private void Awake()
        {
            if (reticle == null)
            {
                reticle = transform as RectTransform;
            }

            if (canvasRect == null && reticle != null)
            {
                canvasRect = reticle.root as RectTransform;
            }
        }

        public void SetNormalizedAim(Vector2 normalizedAim)
        {
            if (reticle == null || canvasRect == null)
            {
                return;
            }

            float x = Mathf.Lerp(-canvasRect.rect.width * 0.5f, canvasRect.rect.width * 0.5f, Mathf.Clamp01(normalizedAim.x));
            float y = Mathf.Lerp(-canvasRect.rect.height * 0.5f, canvasRect.rect.height * 0.5f, Mathf.Clamp01(1f - normalizedAim.y));
            reticle.anchoredPosition = new Vector2(x, y);
        }
    }
}
