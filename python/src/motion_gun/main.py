from __future__ import annotations

import argparse
import time

from .config import GestureConfig
from .feature_extractor import extract_frame_features
from .gesture_engine import GestureEngine
from .transport import UdpJsonSender
from .vision import MediaPipeHandTracker

try:
    import cv2
except ImportError:  # pragma: no cover - runtime only
    cv2 = None


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Motion gun game gesture sender")
    parser.add_argument("--camera-index", type=int, default=0)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5053)
    parser.add_argument("--primary-label", choices=["Left", "Right"], default=None)
    parser.add_argument("--show-preview", action="store_true")
    return parser


def _draw_preview(frame, packet) -> None:
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
        "q quit / c calibrate",
        (16, 84),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.55,
        (255, 255, 255),
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


def main() -> None:
    if cv2 is None:
        raise RuntimeError("OpenCV is not installed. Install requirements.txt first.")

    args = build_parser().parse_args()
    camera = cv2.VideoCapture(args.camera_index)
    if not camera.isOpened():
        raise RuntimeError(f"Could not open camera index {args.camera_index}.")

    config = GestureConfig(primary_hand_label=args.primary_label)
    engine = GestureEngine(config)
    tracker = MediaPipeHandTracker()
    sender = UdpJsonSender(args.host, args.port)

    try:
        while True:
            ok, frame = camera.read()
            if not ok:
                break

            timestamp_seconds = time.monotonic()
            observations = tracker.detect(frame, int(timestamp_seconds * 1000))
            features = extract_frame_features(observations, timestamp_seconds)
            packet = engine.process_frame(features)
            sender.send(packet)

            if args.show_preview:
                _draw_preview(frame, packet)
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
