from __future__ import annotations

import argparse
from pathlib import Path
import platform

from .config import GestureConfig
from .feature_extractor import extract_frame_features
from .gesture_engine import GestureEngine
from .runtime import MonotonicTimeSource, process_feature_frame
from .transport import UdpJsonSender
from .vision import MediaPipeHandTracker

try:
    import cv2
except ImportError:  # pragma: no cover - runtime only
    cv2 = None


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Motion gun game gesture sender")
    parser.add_argument("--camera-index", type=int, default=0)
    parser.add_argument(
        "--camera-backend",
        choices=["auto", "dshow", "msmf", "default"],
        default="auto",
    )
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5053)
    parser.add_argument("--config")
    parser.add_argument("--save-config")
    parser.add_argument("--primary-label", choices=["Left", "Right"], default=None)
    parser.add_argument("--show-preview", action="store_true")
    return parser


def _open_camera(camera_index: int, backend_mode: str):
    if cv2 is None:
        raise RuntimeError("OpenCV is not installed. Install requirements.txt first.")

    backend_candidates: list[tuple[str, int | None]] = _camera_backend_candidates(backend_mode)

    errors: list[str] = []
    for backend_name, backend_flag in backend_candidates:
        camera = (
            cv2.VideoCapture(camera_index)
            if backend_flag is None
            else cv2.VideoCapture(camera_index, backend_flag)
        )

        if not camera.isOpened():
            camera.release()
            errors.append(f"{backend_name}: open failed")
            continue

        frame_stats = []
        valid_frame = None
        for _ in range(15):
            ok, frame = camera.read()
            if not ok or frame is None:
                continue

            mean_value = float(frame.mean())
            max_value = int(frame.max())
            frame_stats.append((mean_value, max_value))
            if max_value > 0:
                valid_frame = frame
                break

        if valid_frame is not None:
            return camera, backend_name

        camera.release()
        if frame_stats:
            last_mean, last_max = frame_stats[-1]
            errors.append(
                f"{backend_name}: frames were black (mean={last_mean:.1f}, max={last_max})"
            )
        else:
            errors.append(f"{backend_name}: opened but could not read frames")

    error_summary = "; ".join(errors) if errors else "no backend attempts were made"
    raise RuntimeError(
        "Could not read frames from camera index "
        f"{camera_index}. Tried backends: {error_summary}. "
        "Close other apps using the camera, open the Windows Camera app to verify the device, "
        "check OS camera permissions/privacy shutter, or try a different camera index/backend."
    )


def _camera_backend_candidates(backend_mode: str) -> list[tuple[str, int | None]]:
    if platform.system() != "Windows":
        return [("default", None)]

    backend_lookup = {
        "dshow": ("dshow", getattr(cv2, "CAP_DSHOW", None)),
        "msmf": ("msmf", getattr(cv2, "CAP_MSMF", None)),
        "default": ("default", None),
    }
    if backend_mode == "auto":
        return [
            backend_lookup["dshow"],
            backend_lookup["msmf"],
            backend_lookup["default"],
        ]
    return [backend_lookup[backend_mode]]


def _draw_preview(frame, packet, features, config: GestureConfig, status_text: str) -> None:
    if cv2 is None:
        return

    status = "TRACKING" if packet.primary_hand_detected else "SEARCHING"
    cv2.putText(
        frame,
        f"status: {status}",
        (16, 28),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.7,
        (40, 220, 80) if packet.primary_hand_detected else (0, 160, 255),
        2,
    )
    cv2.putText(
        frame,
        f"confidence: {packet.tracking_confidence:.2f}",
        (16, 56),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.6,
        (255, 255, 255),
        1,
    )
    cv2.putText(
        frame,
        "q quit / c calibrate / p save / [ ] track / - = fire / , . smooth",
        (16, 84),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.45,
        (255, 255, 255),
        1,
    )
    cv2.putText(
        frame,
        f"hands: {len(features.hands)}",
        (16, 112),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.55,
        (255, 255, 255),
        1,
    )

    event_label = ""
    if packet.fire:
        event_label = "FIRE"
    elif packet.reload:
        event_label = "RELOAD"
    elif packet.weapon_slot > 0:
        event_label = f"WEAPON {packet.weapon_slot}"

    if event_label:
        cv2.putText(
            frame,
            event_label,
            (16, 148),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.8,
            (0, 210, 255),
            2,
        )

    for index, hand in enumerate(features.hands[:2]):
        cv2.putText(
            frame,
            (
                f"{hand.label} gun={hand.gun_pose_score:.2f} "
                f"trigger={hand.trigger_curl:.2f} fingers={hand.finger_count}"
            ),
            (16, 188 + (index * 28)),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.5,
            (255, 255, 255),
            1,
        )

    cv2.putText(
        frame,
        (
            f"track_min={config.min_tracking_confidence:.2f} "
            f"fire_trigger={config.fire_trigger_threshold:.2f} "
            f"smoothing={config.aim_smoothing:.2f}"
        ),
        (16, 248),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.5,
        (180, 240, 255),
        1,
    )
    cv2.putText(
        frame,
        status_text,
        (16, 276),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.5,
        (80, 220, 255),
        1,
    )

    height, width = frame.shape[:2]
    aim_x = int(packet.aim_x * width)
    aim_y = int(packet.aim_y * height)
    cv2.drawMarker(
        frame,
        (aim_x, aim_y),
        (0, 255, 255),
        markerType=cv2.MARKER_CROSS,
        markerSize=20,
        thickness=2,
    )


def _apply_adjustment(config: GestureConfig, key: int) -> str | None:
    if key == ord("["):
        config.min_tracking_confidence = _clamp_step(config.min_tracking_confidence - 0.02)
        return f"min_tracking_confidence={config.min_tracking_confidence:.2f}"
    if key == ord("]"):
        config.min_tracking_confidence = _clamp_step(config.min_tracking_confidence + 0.02)
        return f"min_tracking_confidence={config.min_tracking_confidence:.2f}"
    if key == ord("-"):
        config.fire_trigger_threshold = _clamp_step(config.fire_trigger_threshold - 0.02)
        return f"fire_trigger_threshold={config.fire_trigger_threshold:.2f}"
    if key == ord("="):
        config.fire_trigger_threshold = _clamp_step(config.fire_trigger_threshold + 0.02)
        return f"fire_trigger_threshold={config.fire_trigger_threshold:.2f}"
    if key == ord(","):
        config.aim_smoothing = _clamp_step(config.aim_smoothing - 0.02)
        return f"aim_smoothing={config.aim_smoothing:.2f}"
    if key == ord("."):
        config.aim_smoothing = _clamp_step(config.aim_smoothing + 0.02)
        return f"aim_smoothing={config.aim_smoothing:.2f}"
    return None


def _clamp_step(value: float) -> float:
    return max(0.0, min(1.0, round(value, 2)))


def _resolve_save_config_path(args) -> Path:
    if args.save_config:
        return Path(args.save_config)
    if args.config:
        return Path(args.config)
    return Path(__file__).resolve().parents[2] / "configs" / "live_camera_config.json"


def main() -> None:
    if cv2 is None:
        raise RuntimeError("OpenCV is not installed. Install requirements.txt first.")

    args = build_parser().parse_args()
    camera, camera_backend = _open_camera(args.camera_index, args.camera_backend)

    config = GestureConfig.from_json_file(args.config) if args.config else GestureConfig()
    if args.primary_label is not None:
        config.primary_hand_label = args.primary_label

    engine = GestureEngine(config)
    tracker = MediaPipeHandTracker()
    sender = UdpJsonSender(args.host, args.port)
    time_source = MonotonicTimeSource()
    status_text = f"READY ({camera_backend})"
    status_expires_at = float("-inf")
    save_config_path = _resolve_save_config_path(args)

    try:
        while True:
            ok, frame = camera.read()
            if not ok:
                break

            timestamp_seconds = time_source.now()
            observations = tracker.detect(frame, int(timestamp_seconds * 1000))
            features = extract_frame_features(observations, timestamp_seconds)
            packet = process_feature_frame(engine, features, sender)

            if args.show_preview:
                if time_source.now() > status_expires_at:
                    status_text = "READY"

                _draw_preview(frame, packet, features, config, status_text)
                cv2.imshow("Motion Gun Camera", frame)
                key = cv2.waitKey(1) & 0xFF
                if key == ord("q"):
                    break
                if key == ord("c") and packet.primary_hand_detected:
                    primary_label = max(
                        features.hands,
                        key=lambda hand: hand.gun_pose_score,
                    ).label
                    engine.set_primary_hand_label(primary_label)
                    config.primary_hand_label = primary_label
                    status_text = f"PRIMARY={primary_label}"
                    status_expires_at = time_source.now() + 2.0
                elif key == ord("p"):
                    saved_path = config.save_json_file(save_config_path)
                    status_text = f"SAVED {saved_path.name}"
                    status_expires_at = time_source.now() + 2.0
                else:
                    adjustment = _apply_adjustment(config, key)
                    if adjustment is not None:
                        status_text = adjustment
                        status_expires_at = time_source.now() + 2.0
            else:
                if (cv2.waitKey(1) & 0xFF) == ord("q"):
                    break
    finally:
        sender.close()
        tracker.close()
        camera.release()
        cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
