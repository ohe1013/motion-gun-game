from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class GestureConfig:
    primary_hand_label: str | None = None
    min_tracking_confidence: float = 0.45
    aim_smoothing: float = 0.35
    fire_trigger_threshold: float = 0.58
    fire_release_threshold: float = 0.34
    fire_cooldown_seconds: float = 0.18
    reload_window_seconds: float = 0.45
    reload_vertical_distance: float = 0.16
    reload_horizontal_tolerance: float = 0.24
    reload_cooldown_seconds: float = 0.8
    reload_start_margin: float = 0.06
    reload_finish_margin: float = 0.04
    weapon_hold_seconds: float = 0.35
    slot_mapping: dict[int, int] = field(default_factory=lambda: {1: 1, 2: 2, 3: 3})
