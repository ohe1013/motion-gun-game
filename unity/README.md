# Unity Wiring

Copy `Assets/Scripts/MotionGun` into your Unity project.

## Fast path

Use the editor menu item `MotionGun > Create Demo Scene`.

That menu creates:

- a camera with `UdpGestureClient`
- a `MotionGunController` object with three example weapons and a `RangeSessionController`
- a primitive-based first-person weapon visual set attached to the weapon pivot
- a HUD canvas with weapon, ammo, confidence, score, accuracy, wave, timer, remaining targets, event text, a banner, and a reticle
- a ground plane, backdrop, and a reusable six-target pool for the wave loop

## Session flow

- Wait for a fresh tracked signal, then fire once to start.
- Clear 4 waves before the 90 second global timer expires.
- The timer pauses during `NO SIGNAL` or lost primary-hand tracking.
- `WaveIntro` banners appear between waves for 1.25 seconds.
- After `TIME UP` or `RANGE CLEAR`, fire again to reset the run.
- The default scene now gives the `Burst` weapon a true 3-shot burst and makes moving and armored targets visually distinct.
- Weapon switching now also swaps visible first-person gun models with different recoil feel per weapon.

## Manual references

If you want to wire the scene yourself, `MotionGunController` needs:

- `Gesture Client`: the `UdpGestureClient` component.
- `Aim Camera`: the main gameplay camera.
- `Weapon Pivot`: optional transform that should rotate toward the aim ray.
- `Tracer`: optional `LineRenderer` used for a short muzzle trace.
- `Muzzle Flash`: optional `ParticleSystem`.
- `Hud`: `RangeHudController` component.
- `Reticle`: `AimReticleController` component.
- `Weapons`: configure 2 to 3 entries with slot ids 1, 2, and 3.

`RangeSessionController` needs:

- `Motion Gun Controller`: the `MotionGunController` instance.
- `Target Pool`: 6 reusable `RangeTarget` objects.
- `Session Duration Seconds`: defaults to `90`.
- `Wave Intro Duration`: defaults to `1.25`.

## Runtime notes

- Python sender defaults to `127.0.0.1:5053`. Match the same port in `UdpGestureClient`.
- `MotionGunController` treats packets older than roughly `0.35s` as stale and shows `NO SIGNAL`.
- Python aim coordinates use image space, so Unity converts `aim_y` internally from top-left origin to viewport space.
- If tracking feels too noisy, raise `Min Tracking Confidence` on `MotionGunController` or adjust thresholds in the Python `GestureConfig`.
- Python threshold tuning can be done with `run_sender.ps1 -ConfigPath .\configs\starter_camera_config.json`.
- If Unity imports TextMeshPro essentials on first open, complete that import before creating the demo scene.

## Harness seams

- `UdpGestureClient` now implements `IGesturePacketSource`, so tests can replace live UDP with a fake packet source.
- `MotionGunController` exposes `SetGesturePacketSource(...)` and `SetTimeSource(...)` for deterministic PlayMode tests.
- `RangeSessionController` exposes `SetTimeSource(...)` so wave/timer progression can be advanced without relying on wall-clock time.
