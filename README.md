# Motion Gun Game

Camera-driven shooting demo with a Python gesture recognizer and Unity runtime scripts.

## Layout

- `python/`: webcam capture, hand landmark processing, gesture inference, UDP sender, launch scripts, and tests.
- `unity/`: Unity runtime scripts plus an editor bootstrap menu for a demo shooting range scene.

## Interaction Model

- Aim: primary hand wrist + index direction drive a normalized aim point.
- Fire: trigger pull on the primary gun pose emits a one-shot fire event.
- Reload: secondary hand swipes down beneath the primary hand.
- Weapon switch: secondary hand holds 1, 2, or 3 fingers to select a slot.

## Transport

The Python service sends JSON packets over UDP to `127.0.0.1:5053`.

```json
{
  "aim_x": 0.52,
  "aim_y": 0.41,
  "fire": false,
  "reload": false,
  "weapon_slot": -1,
  "tracking_confidence": 0.87,
  "primary_hand_detected": true,
  "secondary_hand_detected": true,
  "timestamp_seconds": 123.456
}
```

## Python runtime

This workspace already contains a ready virtual environment at `python/.venv` with `mediapipe`, `opencv-python`, and `numpy` installed.

Quick commands:

```powershell
cd E:\workspace\git\motion-gun-game\python
.\run_tests.ps1
.\run_sender.ps1 -ShowPreview
```

On first run, the sender downloads the official MediaPipe hand landmarker model to `python/models/hand_landmarker.task`.

Useful sender options:

- `-CameraIndex 0`
- `-ServerHost 127.0.0.1`
- `-Port 5053`
- `-ConfigPath .\configs\starter_camera_config.json`
- `-PrimaryLabel Right`
- `-ShowPreview`

You can tune gesture thresholds without editing code by copying `python/configs/starter_camera_config.json` and passing it through `-ConfigPath`.

The preview now shows current hand count, per-hand debug values, and transient `FIRE` / `RELOAD` / `WEAPON` events to help calibration.

Preview controls:

- `q`: quit
- `c`: calibrate the currently tracked gun hand as primary

## Unity setup

1. Create or open a Unity 3D Core project.
2. Copy `unity/Assets/Scripts/MotionGun` into `Assets/Scripts`.
3. In Unity, run `MotionGun > Create Demo Scene` from the top menu.
4. Press Play while the Python sender is running.

The generated demo scene now builds a wave-clear range MVP:

- First valid fire starts the run.
- Clear 4 waves before the 90 second timer expires.
- The timer pauses whenever signal tracking is lost.
- After `TIME UP` or `RANGE CLEAR`, fire again to restart.
- The HUD shows weapon/ammo, tracking, score, accuracy, wave, timer, remaining targets, and session banner text.

More scene wiring notes are in `unity/README.md`.
