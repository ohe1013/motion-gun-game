using TMPro;
using UnityEngine;

namespace MotionGun.UI
{
    public class RangeHudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text weaponLabel;
        [SerializeField] private TMP_Text ammoLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text confidenceLabel;

        public void RenderSnapshot(
            string weaponName,
            int ammo,
            int magazineSize,
            string status,
            float confidence
        )
        {
            if (weaponLabel != null)
            {
                weaponLabel.text = weaponName;
            }

            if (ammoLabel != null)
            {
                ammoLabel.text = $"{ammo} / {magazineSize}";
            }

            if (statusLabel != null)
            {
                statusLabel.text = status;
            }

            if (confidenceLabel != null)
            {
                confidenceLabel.text = $"TRACK {confidence:0.00}";
            }
        }
    }
}
