from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
if str(SRC) not in sys.path:
    sys.path.insert(0, str(SRC))

from motion_gun.harness import MemoryPacketSink, load_scenario_fixture, replay_fixture_packets


class HarnessFixtureTests(unittest.TestCase):
    def test_fixture_loader_builds_frames_and_expected_packets(self) -> None:
        fixture = load_scenario_fixture(ROOT / "tests" / "fixtures" / "fire_and_reload.json")

        self.assertEqual(len(fixture.frames), 3)
        self.assertEqual(fixture.frames[1].hands[0].label, "Right")
        self.assertEqual(fixture.expected_packets[1]["fire"], True)

    def test_fixture_replay_matches_expected_packet_fields(self) -> None:
        fixture = load_scenario_fixture(ROOT / "tests" / "fixtures" / "fire_and_reload.json")
        sink = MemoryPacketSink()

        packets = replay_fixture_packets(fixture, packet_sink=sink)

        self.assertEqual(len(packets), len(fixture.expected_packets))
        self.assertEqual(len(sink.packets), len(fixture.expected_packets))
        for packet, expected in zip(packets, fixture.expected_packets):
            payload = packet.to_payload()
            for key, value in expected.items():
                self.assertEqual(payload[key], value, key)

    def test_fixture_replay_covers_weapon_switch_and_tracking_recovery(self) -> None:
        fixture = load_scenario_fixture(
            ROOT / "tests" / "fixtures" / "weapon_switch_and_tracking_loss.json"
        )

        packets = replay_fixture_packets(fixture)

        self.assertEqual(packets[2].weapon_slot, 2)
        self.assertFalse(packets[3].primary_hand_detected)
        self.assertFalse(packets[4].fire)
        self.assertTrue(packets[6].fire)


if __name__ == "__main__":
    unittest.main()
