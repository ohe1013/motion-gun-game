from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class Landmark:
    x: float
    y: float
    z: float = 0.0


@dataclass(frozen=True, slots=True)
class HandObservation:
    label: str
    score: float
    landmarks: tuple[Landmark, ...]


@dataclass(frozen=True, slots=True)
class HandFeatures:
    label: str
    confidence: float
    gun_pose_score: float
    is_gun_pose: bool
    trigger_curl: float
    finger_count: int
    aim_origin: tuple[float, float]
    aim_target: tuple[float, float]
    palm_center: tuple[float, float]


@dataclass(frozen=True, slots=True)
class FrameFeatures:
    timestamp_seconds: float
    hands: tuple[HandFeatures, ...]


@dataclass(frozen=True, slots=True)
class GesturePacket:
    aim_x: float
    aim_y: float
    fire: bool
    reload: bool
    weapon_slot: int
    tracking_confidence: float
    primary_hand_detected: bool
    secondary_hand_detected: bool
    timestamp_seconds: float

    def to_payload(self) -> dict[str, float | bool | int]:
        return {
            "aim_x": self.aim_x,
            "aim_y": self.aim_y,
            "fire": self.fire,
            "reload": self.reload,
            "weapon_slot": self.weapon_slot,
            "tracking_confidence": self.tracking_confidence,
            "primary_hand_detected": self.primary_hand_detected,
            "secondary_hand_detected": self.secondary_hand_detected,
            "timestamp_seconds": self.timestamp_seconds,
        }
