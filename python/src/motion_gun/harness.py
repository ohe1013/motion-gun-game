from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Mapping

from .config import GestureConfig
from .models import FrameFeatures, GesturePacket, HandFeatures
from .runtime import PacketSink, run_feature_sequence


@dataclass(frozen=True, slots=True)
class GestureScenarioFixture:
    config: GestureConfig
    frames: tuple[FrameFeatures, ...]
    expected_packets: tuple[Mapping[str, Any], ...]


class MemoryPacketSink(PacketSink):
    def __init__(self) -> None:
        self.packets: list[GesturePacket] = []

    def send(self, packet: GesturePacket) -> None:
        self.packets.append(packet)


def hand_from_dict(raw: Mapping[str, Any]) -> HandFeatures:
    return HandFeatures(
        label=str(raw["label"]),
        confidence=float(raw.get("confidence", 0.95)),
        gun_pose_score=float(raw.get("gun_pose_score", 0.9)),
        is_gun_pose=bool(raw.get("is_gun_pose", True)),
        trigger_curl=float(raw.get("trigger_curl", 0.2)),
        finger_count=int(raw.get("finger_count", 0)),
        aim_origin=_point_from_raw(raw.get("aim_origin", raw.get("aim_target", (0.5, 0.5)))),
        aim_target=_point_from_raw(raw.get("aim_target", (0.5, 0.5))),
        palm_center=_point_from_raw(raw.get("palm_center", (0.5, 0.5))),
    )


def frame_from_dict(raw: Mapping[str, Any]) -> FrameFeatures:
    return FrameFeatures(
        timestamp_seconds=float(raw["timestamp_seconds"]),
        hands=tuple(hand_from_dict(hand) for hand in raw.get("hands", ())),
    )


def load_scenario_fixture(path: str | Path) -> GestureScenarioFixture:
    with Path(path).open("r", encoding="utf-8") as handle:
        payload = json.load(handle)

    return GestureScenarioFixture(
        config=GestureConfig.from_dict(payload.get("config", {})),
        frames=tuple(frame_from_dict(frame) for frame in payload.get("frames", ())),
        expected_packets=tuple(payload.get("expected_packets", ())),
    )


def replay_fixture_packets(
    fixture: GestureScenarioFixture,
    *,
    packet_sink: PacketSink | None = None,
) -> list[GesturePacket]:
    from .gesture_engine import GestureEngine

    return run_feature_sequence(
        GestureEngine(fixture.config),
        fixture.frames,
        packet_sink=packet_sink,
    )


def _point_from_raw(raw: Any) -> tuple[float, float]:
    x, y = raw
    return (float(x), float(y))
