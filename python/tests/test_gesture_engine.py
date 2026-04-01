from __future__ import annotations

import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
if str(SRC) not in sys.path:
    sys.path.insert(0, str(SRC))

from motion_gun.config import GestureConfig
from motion_gun.gesture_engine import GestureEngine
from motion_gun.models import FrameFeatures, HandFeatures


def hand(
    *,
    label: str,
    confidence: float = 0.95,
    gun_pose_score: float = 0.9,
    is_gun_pose: bool = True,
    trigger_curl: float = 0.2,
    finger_count: int = 0,
    aim: tuple[float, float] = (0.5, 0.5),
    palm_center: tuple[float, float] = (0.5, 0.5),
) -> HandFeatures:
    return HandFeatures(
        label=label,
        confidence=confidence,
        gun_pose_score=gun_pose_score,
        is_gun_pose=is_gun_pose,
        trigger_curl=trigger_curl,
        finger_count=finger_count,
        aim_origin=aim,
        aim_target=aim,
        palm_center=palm_center,
    )


def frame(timestamp_seconds: float, *hands: HandFeatures) -> FrameFeatures:
    return FrameFeatures(timestamp_seconds=timestamp_seconds, hands=hands)


class GestureEngineTests(unittest.TestCase):
    def test_config_can_load_from_json_file(self) -> None:
        payload = {
            "min_tracking_confidence": 0.72,
            "weapon_hold_seconds": 0.48,
            "slot_mapping": {"1": 2, "2": 3},
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = pathlib.Path(temp_dir) / "gesture.json"
            config_path.write_text(json.dumps(payload), encoding="utf-8")
            config = GestureConfig.from_json_file(config_path)

        self.assertEqual(config.min_tracking_confidence, 0.72)
        self.assertEqual(config.weapon_hold_seconds, 0.48)
        self.assertEqual(config.slot_mapping, {1: 2, 2: 3})

    def test_config_can_round_trip_to_json_file(self) -> None:
        config = GestureConfig(
            primary_hand_label="Right",
            min_tracking_confidence=0.61,
            aim_smoothing=0.42,
            fire_trigger_threshold=0.63,
        )

        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = pathlib.Path(temp_dir) / "roundtrip.json"
            config.save_json_file(config_path)
            loaded = GestureConfig.from_json_file(config_path)

        self.assertEqual(loaded.primary_hand_label, "Right")
        self.assertEqual(loaded.min_tracking_confidence, 0.61)
        self.assertEqual(loaded.aim_smoothing, 0.42)
        self.assertEqual(loaded.fire_trigger_threshold, 0.63)

    def test_fire_requires_release_before_next_shot(self) -> None:
        engine = GestureEngine(GestureConfig())
        primary = hand(label="Right", trigger_curl=0.2)

        packet = engine.process_frame(frame(0.0, primary))
        self.assertFalse(packet.fire)

        packet = engine.process_frame(frame(0.1, hand(label="Right", trigger_curl=0.7)))
        self.assertTrue(packet.fire)

        packet = engine.process_frame(frame(0.25, hand(label="Right", trigger_curl=0.8)))
        self.assertFalse(packet.fire)

        packet = engine.process_frame(frame(0.5, hand(label="Right", trigger_curl=0.2)))
        self.assertFalse(packet.fire)

        packet = engine.process_frame(frame(0.75, hand(label="Right", trigger_curl=0.7)))
        self.assertTrue(packet.fire)

    def test_reload_detects_secondary_swipe_below_primary(self) -> None:
        engine = GestureEngine(GestureConfig())
        primary = hand(label="Right", palm_center=(0.5, 0.46))
        secondary_above = hand(
            label="Left",
            is_gun_pose=False,
            gun_pose_score=0.1,
            finger_count=0,
            palm_center=(0.52, 0.24),
        )
        secondary_below = hand(
            label="Left",
            is_gun_pose=False,
            gun_pose_score=0.1,
            finger_count=0,
            palm_center=(0.52, 0.69),
        )

        packet = engine.process_frame(frame(0.0, primary, secondary_above))
        self.assertFalse(packet.reload)

        packet = engine.process_frame(frame(0.2, primary, secondary_below))
        self.assertTrue(packet.reload)

    def test_weapon_slot_requires_hold_and_latches_until_reset(self) -> None:
        engine = GestureEngine(GestureConfig())
        primary = hand(label="Right")
        secondary = hand(
            label="Left",
            is_gun_pose=False,
            gun_pose_score=0.1,
            finger_count=2,
            palm_center=(0.55, 0.4),
        )

        packet = engine.process_frame(frame(0.0, primary, secondary))
        self.assertEqual(packet.weapon_slot, -1)

        packet = engine.process_frame(frame(0.2, primary, secondary))
        self.assertEqual(packet.weapon_slot, -1)

        packet = engine.process_frame(frame(0.5, primary, secondary))
        self.assertEqual(packet.weapon_slot, 2)

        packet = engine.process_frame(frame(0.8, primary, secondary))
        self.assertEqual(packet.weapon_slot, -1)

        neutral_secondary = hand(
            label="Left",
            is_gun_pose=False,
            gun_pose_score=0.1,
            finger_count=0,
            palm_center=(0.55, 0.4),
        )
        engine.process_frame(frame(1.0, primary, neutral_secondary))
        packet = engine.process_frame(frame(1.6, primary, secondary))
        self.assertEqual(packet.weapon_slot, -1)
        packet = engine.process_frame(frame(2.0, primary, secondary))
        self.assertEqual(packet.weapon_slot, 2)

    def test_tracking_loss_suppresses_shot_until_release(self) -> None:
        engine = GestureEngine(GestureConfig())

        packet = engine.process_frame(
            frame(
                0.0,
                hand(
                    label="Right",
                    confidence=0.1,
                    gun_pose_score=0.56,
                    trigger_curl=0.7,
                ),
            )
        )
        self.assertFalse(packet.primary_hand_detected)
        self.assertFalse(packet.fire)

        packet = engine.process_frame(
            frame(
                0.2,
                hand(
                    label="Right",
                    confidence=0.95,
                    gun_pose_score=0.9,
                    trigger_curl=0.7,
                ),
            )
        )
        self.assertFalse(packet.fire)

        engine.process_frame(frame(0.4, hand(label="Right", trigger_curl=0.2)))
        packet = engine.process_frame(frame(0.7, hand(label="Right", trigger_curl=0.7)))
        self.assertTrue(packet.fire)


if __name__ == "__main__":
    unittest.main()
