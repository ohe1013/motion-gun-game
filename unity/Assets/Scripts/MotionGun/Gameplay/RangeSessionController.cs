using System;
using System.Collections.Generic;
using MotionGun.Runtime;
using UnityEngine;

namespace MotionGun.Gameplay
{
    public class RangeSessionController : MonoBehaviour
    {
        private enum SessionState
        {
            WaitingToStart,
            WaveIntro,
            Running,
            Victory,
            TimeUp,
        }

        [Serializable]
        private class WaveDefinition
        {
            [Min(1)] public int targetCount = 3;
            [Min(0)] public int movingTargetCount;
            [Min(0)] public int armoredTargetCount;
            [Min(1f)] public float armoredHitPoints = 2f;
            [Min(0.1f)] public float movementSpeedMultiplier = 1f;
        }

        [SerializeField] private MotionGunController motionGunController;
        [SerializeField] private List<RangeTarget> targetPool = new List<RangeTarget>();
        [SerializeField] private float sessionDurationSeconds = 90f;
        [SerializeField] private float waveIntroDuration = 1.25f;
        [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

        private SessionState _state = SessionState.WaitingToStart;
        private float _timeRemainingSeconds;
        private float _waveIntroEndsAt;
        private int _currentWaveIndex = -1;
        private int _remainingTargets;
        private IMotionGunTimeSource _timeSource = UnityMotionGunTimeSource.Instance;

        private void Awake()
        {
            if (motionGunController == null)
            {
                motionGunController = GetComponent<MotionGunController>();
            }

            targetPool.RemoveAll(target => target == null);
            if (waves.Count == 0)
            {
                CreateDefaultWaves();
            }
        }

        private void Start()
        {
            foreach (RangeTarget target in targetPool)
            {
                target.DeactivateForWave();
            }

            ResetToWaitingState();
        }

        private void OnEnable()
        {
            if (motionGunController != null)
            {
                motionGunController.FirePulse += HandleFirePulse;
            }

            foreach (RangeTarget target in targetPool)
            {
                target.Cleared += HandleTargetCleared;
            }
        }

        private void OnDisable()
        {
            if (motionGunController != null)
            {
                motionGunController.FirePulse -= HandleFirePulse;
            }

            foreach (RangeTarget target in targetPool)
            {
                target.Cleared -= HandleTargetCleared;
            }
        }

        private void LateUpdate()
        {
            if (motionGunController == null)
            {
                return;
            }

            if (IsTimedState() && motionGunController.TrackingReady)
            {
                _timeRemainingSeconds = Mathf.Max(0f, _timeRemainingSeconds - _timeSource.DeltaTime);
                if (_timeRemainingSeconds <= 0f)
                {
                    EnterOutcome(SessionState.TimeUp, "TIME UP");
                }
            }

            if (_state == SessionState.WaveIntro && _timeSource.Time >= _waveIntroEndsAt)
            {
                BeginCurrentWave();
            }

            PushHudState();
        }

        private void HandleFirePulse()
        {
            if (motionGunController == null)
            {
                return;
            }

            if (_state == SessionState.WaitingToStart)
            {
                motionGunController.ConsumeCurrentFirePulse();
                StartRun();
                return;
            }

            if (_state == SessionState.Victory || _state == SessionState.TimeUp)
            {
                motionGunController.ConsumeCurrentFirePulse();
                StartRun();
            }
        }

        private void HandleTargetCleared(RangeTarget target)
        {
            if (_state != SessionState.Running)
            {
                return;
            }

            _remainingTargets = Mathf.Max(0, _remainingTargets - 1);
            motionGunController.AddScore(100);

            if (_remainingTargets > 0)
            {
                return;
            }

            if (_currentWaveIndex >= waves.Count - 1)
            {
                EnterOutcome(SessionState.Victory, "RANGE CLEAR");
                return;
            }

            EnterWaveIntro(_currentWaveIndex + 1);
        }

        private void StartRun()
        {
            if (motionGunController == null)
            {
                return;
            }

            motionGunController.ResetSession();
            motionGunController.SetCombatActive(false);
            _timeRemainingSeconds = sessionDurationSeconds;
            _currentWaveIndex = -1;
            _remainingTargets = 0;

            foreach (RangeTarget target in targetPool)
            {
                target.DeactivateForWave();
            }

            EnterWaveIntro(0);
        }

        private void BeginCurrentWave()
        {
            if (_currentWaveIndex < 0 || _currentWaveIndex >= waves.Count)
            {
                return;
            }

            WaveDefinition wave = waves[_currentWaveIndex];
            int activeCount = Mathf.Min(Mathf.Max(0, wave.targetCount), targetPool.Count);
            int movingStartIndex = activeCount - Mathf.Min(wave.movingTargetCount, activeCount);
            int armoredStartIndex = activeCount - Mathf.Min(wave.armoredTargetCount, activeCount);

            _remainingTargets = activeCount;
            for (int index = 0; index < targetPool.Count; index++)
            {
                RangeTarget target = targetPool[index];
                if (index >= activeCount)
                {
                    target.DeactivateForWave();
                    continue;
                }

                bool moving = index >= movingStartIndex;
                bool armored = index >= armoredStartIndex;
                float hitPoints = armored ? wave.armoredHitPoints : 1f;
                target.ConfigureForWave(hitPoints, moving, wave.movementSpeedMultiplier);
            }

            _state = SessionState.Running;
            motionGunController.SetCombatActive(true);
            PushHudState();
        }

        private void EnterWaveIntro(int waveIndex)
        {
            _currentWaveIndex = waveIndex;
            _state = SessionState.WaveIntro;
            _waveIntroEndsAt = _timeSource.Time + waveIntroDuration;
            motionGunController.SetCombatActive(false);

            foreach (RangeTarget target in targetPool)
            {
                target.DeactivateForWave();
            }

            if (_currentWaveIndex >= 0 && _currentWaveIndex < waves.Count)
            {
                _remainingTargets = Mathf.Min(waves[_currentWaveIndex].targetCount, targetPool.Count);
            }
            else
            {
                _remainingTargets = 0;
            }

            PushHudState();
        }

        private void EnterOutcome(SessionState state, string banner)
        {
            _state = state;
            _remainingTargets = 0;
            motionGunController.SetCombatActive(false);

            foreach (RangeTarget target in targetPool)
            {
                target.DeactivateForWave();
            }

            motionGunController.ConfigureSessionHud(
                Mathf.Clamp(_currentWaveIndex + 1, 0, waves.Count),
                waves.Count,
                0,
                _timeRemainingSeconds,
                banner
            );
        }

        private void ResetToWaitingState()
        {
            _state = SessionState.WaitingToStart;
            _timeRemainingSeconds = sessionDurationSeconds;
            _currentWaveIndex = -1;
            _remainingTargets = 0;

            if (motionGunController != null)
            {
                motionGunController.ResetSession();
                motionGunController.SetCombatActive(false);
            }

            PushHudState();
        }

        private bool IsTimedState()
        {
            return _state == SessionState.WaveIntro || _state == SessionState.Running;
        }

        private void PushHudState()
        {
            if (motionGunController == null)
            {
                return;
            }

            motionGunController.ConfigureSessionHud(
                Mathf.Clamp(_currentWaveIndex + 1, 0, waves.Count),
                waves.Count,
                _remainingTargets,
                _timeRemainingSeconds,
                GetBannerText()
            );
        }

        private string GetBannerText()
        {
            switch (_state)
            {
                case SessionState.WaitingToStart:
                    return "FIRE TO START";
                case SessionState.WaveIntro:
                    return "WAVE " + (_currentWaveIndex + 1);
                case SessionState.Victory:
                    return "RANGE CLEAR";
                case SessionState.TimeUp:
                    return "TIME UP";
                default:
                    return string.Empty;
            }
        }

        private void CreateDefaultWaves()
        {
            waves = new List<WaveDefinition>
            {
                new WaveDefinition
                {
                    targetCount = 3,
                    movingTargetCount = 0,
                    armoredTargetCount = 0,
                    armoredHitPoints = 2f,
                    movementSpeedMultiplier = 1f,
                },
                new WaveDefinition
                {
                    targetCount = 4,
                    movingTargetCount = 1,
                    armoredTargetCount = 0,
                    armoredHitPoints = 2f,
                    movementSpeedMultiplier = 1f,
                },
                new WaveDefinition
                {
                    targetCount = 5,
                    movingTargetCount = 2,
                    armoredTargetCount = 1,
                    armoredHitPoints = 2f,
                    movementSpeedMultiplier = 1f,
                },
                new WaveDefinition
                {
                    targetCount = 6,
                    movingTargetCount = 3,
                    armoredTargetCount = 2,
                    armoredHitPoints = 2f,
                    movementSpeedMultiplier = 1.5f,
                },
            };
        }

        public void SetTimeSource(IMotionGunTimeSource timeSource)
        {
            _timeSource = timeSource ?? UnityMotionGunTimeSource.Instance;
        }
    }
}
