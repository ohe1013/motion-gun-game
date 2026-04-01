from __future__ import annotations

import time
from collections.abc import Iterable
from typing import Protocol

from .gesture_engine import GestureEngine
from .models import FrameFeatures, GesturePacket


class FeatureFrameSource(Protocol):
    def read(self) -> FrameFeatures | None:
        """Return the next frame, or None when the source is exhausted."""


class PacketSink(Protocol):
    def send(self, packet: GesturePacket) -> None:
        """Consume a gesture packet."""


class TimeSource(Protocol):
    def now(self) -> float:
        """Return the current timestamp in seconds."""


class MonotonicTimeSource:
    def now(self) -> float:
        return time.monotonic()


def process_feature_frame(
    engine: GestureEngine,
    frame: FrameFeatures,
    packet_sink: PacketSink | None = None,
) -> GesturePacket:
    packet = engine.process_frame(frame)
    if packet_sink is not None:
        packet_sink.send(packet)
    return packet


def run_feature_sequence(
    engine: GestureEngine,
    frames: Iterable[FrameFeatures],
    packet_sink: PacketSink | None = None,
) -> list[GesturePacket]:
    return [process_feature_frame(engine, frame, packet_sink) for frame in frames]
