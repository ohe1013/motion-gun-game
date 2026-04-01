"""Motion gun gesture recognition package."""

from .config import GestureConfig
from .gesture_engine import GestureEngine
from .harness import GestureScenarioFixture, MemoryPacketSink, load_scenario_fixture, replay_fixture_packets
from .models import FrameFeatures, GesturePacket, HandFeatures
from .runtime import MonotonicTimeSource, process_feature_frame, run_feature_sequence

__all__ = [
    "FrameFeatures",
    "GestureConfig",
    "GestureEngine",
    "GesturePacket",
    "GestureScenarioFixture",
    "HandFeatures",
    "MemoryPacketSink",
    "MonotonicTimeSource",
    "load_scenario_fixture",
    "process_feature_frame",
    "replay_fixture_packets",
    "run_feature_sequence",
]
