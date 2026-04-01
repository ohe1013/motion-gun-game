# Motion Gun Game Plan

## Product Direction

- Primary goal: make the motion-gun demo easy to run, easy to calibrate, and convincing to play.
- Secondary goal: add just enough harnessing to prevent core regressions while the gameplay experience is still evolving.

## Current Status

### Completed

- Python gesture pipeline is implemented end-to-end: feature extraction, gesture inference, packet serialization, and UDP send.
- Unity gameplay loop is implemented as a playable MVP: packet receive, aiming, firing, reloading, weapon switching, HUD, and wave-based range session.
- Demo weapons now have clearer roles, including a real multi-shot burst weapon.
- The demo scene now shows visible first-person weapon models instead of an invisible pivot-only gun.
- Target variants now read more clearly in play through distinct moving and armored visuals.
- Python regression coverage exists for core gesture rules.
- Python fixture-driven harness replay is in place for `FrameFeatures -> GesturePacket`.
- Unity runtime now exposes replaceable packet and time sources for future deterministic tests.
- Python first-run bootstrap is now scriptable through `python/bootstrap.ps1`.
- Live preview tuning now supports in-session calibration and config save for camera-specific thresholds.

### In Progress

- Project docs are being rewritten around actual usage and progress rather than just code layout.
- Gesture tuning still needs real-camera iteration to settle on strong default thresholds.
- Session pacing and weapon feel still need a dedicated playtest pass.
- Weapon presentation is in place, but visual polish still needs a later art pass or asset replacement.
- Runtime seams are in place, but Unity automated tests are intentionally deferred behind product-facing improvements.

### Not Started

- Guided calibration workflow for different users and cameras.
- Gameplay feel pass on aim stability, fire confidence, reload reliability, and player feedback.
- Unity PlayMode regression tests and replay-driven end-to-end automation.
- CI automation and demo operator checklist.

## Priority Order

### Priority 1: Make Startup Friction Low

- Keep first-run setup explicit and short.
- Ensure dependency install, test run, and sender launch are all documented and scriptable.
- Fail with actionable messages when the environment is incomplete.

### Priority 2: Improve Playability

- Tune aim stability and gesture thresholds with real camera use.
- Reduce false fire, reload misses, and awkward weapon switching.
- Improve on-screen feedback when tracking is lost or the player needs to recalibrate.
- Use preview-time live tuning and config save to produce stable per-camera presets.
- Playtest weapon roles and target readability until each wave feels intentionally different.

### Priority 3: Raise Demo Quality

- Tighten session pacing, weapon feel, and HUD clarity.
- Document a repeatable `camera on -> sender on -> Unity on -> play` operator flow.
- Validate the demo on the target machine and lighting conditions.

### Priority 4: Expand Automation Carefully

- Add Unity PlayMode tests for the most important gameplay states only after the runtime flow is satisfactory.
- Reuse Python fixture scenarios for later replay-based end-to-end checks.
- Add CI only after local setup and runtime flow are stable.

## Verification

### Available Now

```powershell
cd .\python
.\bootstrap.ps1 -RunTests
.\run_tests.ps1
```

- `bootstrap.ps1` creates `.venv`, installs dependencies, and can run the Python test suite.
- `run_tests.ps1` runs the existing Python gesture tests plus the fixture-driven harness tests.

### Manual Runtime Check

```powershell
cd .\python
.\run_sender.ps1 -ShowPreview
```

- Then start the Unity demo scene and confirm aim, fire, reload, weapon switching, and wave progression.

## Notes

- This file is the project-level progress tracker.
- Update this file when the product priority or milestone state changes in a meaningful way.
