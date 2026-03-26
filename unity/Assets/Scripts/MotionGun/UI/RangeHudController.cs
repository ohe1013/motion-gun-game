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
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text eventLabel;

        public void RenderSnapshot(
            string weaponName,
            int ammo,
            int magazineSize,
            string status,
            float confidence,
            int score,
            int shotsFired,
            int shotsHit,
            string eventText
        )
        {
            if (weaponLabel != null)
            {
                weaponLabel.text = weaponName;
            }

            if (ammoLabel != null)
            {
                ammoLabel.text = string.Format("{0} / {1}", ammo, magazineSize);
            }

            if (statusLabel != null)
            {
                statusLabel.text = status;
            }

            if (confidenceLabel != null)
            {
                confidenceLabel.text = string.Format("TRACK {0:0.00}", confidence);
            }

            if (scoreLabel != null)
            {
                float accuracy = shotsFired > 0 ? ((float)shotsHit / shotsFired) * 100f : 0f;
                scoreLabel.text = string.Format(
                    "SCORE {0}  HIT {1}/{2}  {3:0}%",
                    score,
                    shotsHit,
                    shotsFired,
                    accuracy
                );
            }

            if (eventLabel != null)
            {
                eventLabel.text = eventText;
            }
        }
    }
}
