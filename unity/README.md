# Unity Wiring

Copy `Assets/Scripts/MotionGun` into your Unity project.

## Fast path

Use the editor menu item `MotionGun > Create Demo Scene`.

That menu creates:

- a camera with `UdpGestureClient`
- a `MotionGunController` object with three example weapons
- a HUD canvas with text labels and a reticle
- a ground plane, backdrop, and three example targets

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

## Runtime notes

- Python sender defaults to `127.0.0.1:5053`. Match the same port in `UdpGestureClient`.
- Python aim coordinates use image space, so Unity converts `aim_y` internally from top-left origin to viewport space.
- If tracking feels too noisy, raise `Min Tracking Confidence` on `MotionGunController` or adjust thresholds in the Python `GestureConfig`.
- If Unity imports TextMeshPro essentials on first open, complete that import before creating the demo scene.
