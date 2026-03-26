from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Mapping


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

    @classmethod
    def from_dict(cls, raw: Mapping[str, Any]) -> "GestureConfig":
        defaults = cls()
        slot_mapping = raw.get("slot_mapping")
        normalized_slot_mapping: dict[int, int] | None = None
        if slot_mapping is not None:
            normalized_slot_mapping = {
                int(source): int(target) for source, target in slot_mapping.items()
            }

        return cls(
            primary_hand_label=raw.get("primary_hand_label"),
            min_tracking_confidence=float(
                raw.get("min_tracking_confidence", defaults.min_tracking_confidence)
            ),
            aim_smoothing=float(raw.get("aim_smoothing", defaults.aim_smoothing)),
            fire_trigger_threshold=float(
                raw.get("fire_trigger_threshold", defaults.fire_trigger_threshold)
            ),
            fire_release_threshold=float(
                raw.get("fire_release_threshold", defaults.fire_release_threshold)
            ),
            fire_cooldown_seconds=float(
                raw.get("fire_cooldown_seconds", defaults.fire_cooldown_seconds)
            ),
            reload_window_seconds=float(
                raw.get("reload_window_seconds", defaults.reload_window_seconds)
            ),
            reload_vertical_distance=float(
                raw.get("reload_vertical_distance", defaults.reload_vertical_distance)
            ),
            reload_horizontal_tolerance=float(
                raw.get(
                    "reload_horizontal_tolerance",
                    defaults.reload_horizontal_tolerance,
                )
            ),
            reload_cooldown_seconds=float(
                raw.get("reload_cooldown_seconds", defaults.reload_cooldown_seconds)
            ),
            reload_start_margin=float(
                raw.get("reload_start_margin", defaults.reload_start_margin)
            ),
            reload_finish_margin=float(
                raw.get("reload_finish_margin", defaults.reload_finish_margin)
            ),
            weapon_hold_seconds=float(
                raw.get("weapon_hold_seconds", defaults.weapon_hold_seconds)
            ),
            slot_mapping=normalized_slot_mapping or defaults.slot_mapping,
        )

    @classmethod
    def from_json_file(cls, path: str | Path) -> "GestureConfig":
        with Path(path).open("r", encoding="utf-8") as handle:
            return cls.from_dict(json.load(handle))
