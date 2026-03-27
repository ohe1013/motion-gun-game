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
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text remainingTargetsLabel;
        [SerializeField] private TMP_Text bannerLabel;

        public void RenderSnapshot(
            string weaponName,
            int ammo,
            int magazineSize,
            string status,
            float confidence,
            int score,
            int shotsFired,
            int shotsHit,
            string eventText,
            int currentWave,
            int totalWaves,
            int remainingTargets,
            float timeRemainingSeconds,
            string sessionBanner
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

            if (waveLabel != null)
            {
                waveLabel.text = totalWaves > 0
                    ? string.Format("WAVE {0}/{1}", Mathf.Max(currentWave, 0), totalWaves)
                    : "WAVE --";
            }

            if (timerLabel != null)
            {
                int secondsRemaining = Mathf.Max(0, Mathf.CeilToInt(timeRemainingSeconds));
                timerLabel.text = string.Format(
                    "TIME {0:00}:{1:00}",
                    secondsRemaining / 60,
                    secondsRemaining % 60
                );
            }

            if (remainingTargetsLabel != null)
            {
                remainingTargetsLabel.text = string.Format("TARGETS {0}", Mathf.Max(remainingTargets, 0));
            }

            if (bannerLabel != null)
            {
                bannerLabel.text = sessionBanner;
            }
        }
    }
}
