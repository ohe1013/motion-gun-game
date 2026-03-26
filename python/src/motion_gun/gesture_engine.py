from __future__ import annotations

from dataclasses import dataclass

from .config import GestureConfig
from .models import FrameFeatures, GesturePacket, HandFeatures


@dataclass(slots=True)
class _ReloadCandidate:
    started_at: float
    start_y: float


class GestureEngine:
    def __init__(self, config: GestureConfig | None = None) -> None:
        self.config = config or GestureConfig()
        self._primary_hand_label = self.config.primary_hand_label
        self._smoothed_aim: tuple[float, float] | None = None
        self._fire_armed = True
        self._last_fire_time = -1e9
        self._last_reload_time = -1e9
        self._reload_candidate: _ReloadCandidate | None = None
        self._slot_candidate_count: int | None = None
        self._slot_candidate_started_at = 0.0
        self._slot_latched = False

    def set_primary_hand_label(self, label: str | None) -> None:
        self._primary_hand_label = label

    def process_frame(self, frame: FrameFeatures) -> GesturePacket:
        primary, secondary = self._resolve_hands(frame.hands)
        aim_x, aim_y = self._resolve_aim(primary)
        tracking_confidence = self._tracking_confidence(primary)
        tracking_ready = (
            primary is not None and tracking_confidence >= self.config.min_tracking_confidence
        )

        fire = False
        reload = False
        weapon_slot = -1

        if tracking_ready:
            fire = self._detect_fire(primary, frame.timestamp_seconds)
            if secondary is not None:
                reload = self._detect_reload(primary, secondary, frame.timestamp_seconds)
                weapon_slot = self._detect_weapon_slot(secondary, frame.timestamp_seconds)
            else:
                self._clear_secondary_state()
        else:
            self._fire_armed = False
            self._clear_secondary_state()

        return GesturePacket(
            aim_x=aim_x,
            aim_y=aim_y,
            fire=fire,
            reload=reload,
            weapon_slot=weapon_slot,
            tracking_confidence=tracking_confidence,
            primary_hand_detected=tracking_ready,
            secondary_hand_detected=secondary is not None,
            timestamp_seconds=frame.timestamp_seconds,
        )

    def _resolve_hands(
        self, hands: tuple[HandFeatures, ...]
    ) -> tuple[HandFeatures | None, HandFeatures | None]:
        if not hands:
            return None, None

        primary: HandFeatures | None = None
        secondary: HandFeatures | None = None

        if self._primary_hand_label is not None:
            for hand in hands:
                if hand.label == self._primary_hand_label:
                    primary = hand
                else:
                    secondary = hand
            if primary is None:
                return None, None
            return primary, secondary

        primary = max(hands, key=lambda hand: hand.gun_pose_score)
        if not primary.is_gun_pose:
            return None, None
        for hand in hands:
            if hand is not primary:
                secondary = hand
                break
        return primary, secondary

    def _resolve_aim(self, primary: HandFeatures | None) -> tuple[float, float]:
        if primary is None:
            if self._smoothed_aim is None:
                self._smoothed_aim = (0.5, 0.5)
            return self._smoothed_aim

        raw_x, raw_y = primary.aim_target
        if self._smoothed_aim is None:
            self._smoothed_aim = (raw_x, raw_y)
            return self._smoothed_aim

        alpha = self.config.aim_smoothing
        smoothed = (
            ((1.0 - alpha) * self._smoothed_aim[0]) + (alpha * raw_x),
            ((1.0 - alpha) * self._smoothed_aim[1]) + (alpha * raw_y),
        )
        self._smoothed_aim = smoothed
        return smoothed

    def _tracking_confidence(self, primary: HandFeatures | None) -> float:
        if primary is None:
            return 0.0
        return min(1.0, (0.55 * primary.confidence) + (0.45 * primary.gun_pose_score))

    def _detect_fire(self, primary: HandFeatures, timestamp_seconds: float) -> bool:
        if primary.trigger_curl <= self.config.fire_release_threshold:
            self._fire_armed = True

        if not primary.is_gun_pose:
            return False
        if not self._fire_armed:
            return False
        if primary.trigger_curl < self.config.fire_trigger_threshold:
            return False
        if timestamp_seconds - self._last_fire_time < self.config.fire_cooldown_seconds:
            return False

        self._fire_armed = False
        self._last_fire_time = timestamp_seconds
        return True

    def _detect_reload(
        self,
        primary: HandFeatures,
        secondary: HandFeatures,
        timestamp_seconds: float,
    ) -> bool:
        if timestamp_seconds - self._last_reload_time < self.config.reload_cooldown_seconds:
            return False

        x_distance = abs(primary.palm_center[0] - secondary.palm_center[0])
        if x_distance > self.config.reload_horizontal_tolerance:
            self._reload_candidate = None
            return False

        secondary_y = secondary.palm_center[1]
        primary_y = primary.palm_center[1]

        if secondary_y < (primary_y - self.config.reload_start_margin):
            if self._reload_candidate is None:
                self._reload_candidate = _ReloadCandidate(
                    started_at=timestamp_seconds,
                    start_y=secondary_y,
                )
            return False

        if self._reload_candidate is None:
            return False

        elapsed = timestamp_seconds - self._reload_candidate.started_at
        moved_down = secondary_y - self._reload_candidate.start_y
        finished_below_primary = secondary_y > (primary_y + self.config.reload_finish_margin)

        if elapsed > self.config.reload_window_seconds:
            self._reload_candidate = None
            return False

        if finished_below_primary and moved_down >= self.config.reload_vertical_distance:
            self._reload_candidate = None
            self._last_reload_time = timestamp_seconds
            return True

        return False

    def _detect_weapon_slot(
        self, secondary: HandFeatures, timestamp_seconds: float
    ) -> int:
        mapped_slot = self.config.slot_mapping.get(secondary.finger_count)
        if mapped_slot is None:
            self._slot_candidate_count = None
            self._slot_latched = False
            return -1

        if self._slot_candidate_count != secondary.finger_count:
            self._slot_candidate_count = secondary.finger_count
            self._slot_candidate_started_at = timestamp_seconds
            self._slot_latched = False
            return -1

        if self._slot_latched:
            return -1

        if timestamp_seconds - self._slot_candidate_started_at >= self.config.weapon_hold_seconds:
            self._slot_latched = True
            return mapped_slot

        return -1

    def _clear_secondary_state(self) -> None:
        self._reload_candidate = None
        self._slot_candidate_count = None
        self._slot_latched = False
