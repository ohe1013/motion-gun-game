from __future__ import annotations

from math import hypot

from .models import FrameFeatures, HandFeatures, HandObservation, Landmark

WRIST = 0
THUMB_MCP = 2
THUMB_IP = 3
THUMB_TIP = 4
INDEX_MCP = 5
INDEX_PIP = 6
INDEX_TIP = 8
MIDDLE_MCP = 9
MIDDLE_PIP = 10
MIDDLE_TIP = 12
RING_MCP = 13
RING_PIP = 14
RING_TIP = 16
PINKY_MCP = 17
PINKY_PIP = 18
PINKY_TIP = 20


def _distance(a: Landmark, b: Landmark) -> float:
    return hypot(a.x - b.x, a.y - b.y)


def _clamp(value: float, lower: float = 0.0, upper: float = 1.0) -> float:
    return max(lower, min(upper, value))


def _extension_value(
    landmarks: tuple[Landmark, ...],
    *,
    tip_index: int,
    pip_index: int,
    mcp_index: int,
) -> float:
    wrist = landmarks[WRIST]
    palm_size = max(_distance(landmarks[MIDDLE_MCP], wrist), 1e-4)
    tip_distance = _distance(landmarks[tip_index], wrist)
    pip_distance = _distance(landmarks[pip_index], wrist)
    mcp_distance = _distance(landmarks[mcp_index], wrist)
    raw = (tip_distance - max(pip_distance, mcp_distance)) / palm_size
    return _clamp(raw * 2.5)


def extract_hand_features(observation: HandObservation) -> HandFeatures:
    landmarks = observation.landmarks

    thumb_extension = _extension_value(
        landmarks,
        tip_index=THUMB_TIP,
        pip_index=THUMB_IP,
        mcp_index=THUMB_MCP,
    )
    index_extension = _extension_value(
        landmarks,
        tip_index=INDEX_TIP,
        pip_index=INDEX_PIP,
        mcp_index=INDEX_MCP,
    )
    middle_extension = _extension_value(
        landmarks,
        tip_index=MIDDLE_TIP,
        pip_index=MIDDLE_PIP,
        mcp_index=MIDDLE_MCP,
    )
    ring_extension = _extension_value(
        landmarks,
        tip_index=RING_TIP,
        pip_index=RING_PIP,
        mcp_index=RING_MCP,
    )
    pinky_extension = _extension_value(
        landmarks,
        tip_index=PINKY_TIP,
        pip_index=PINKY_PIP,
        mcp_index=PINKY_MCP,
    )

    support_finger_curl = (
        (1.0 - middle_extension) + (1.0 - ring_extension) + (1.0 - pinky_extension)
    ) / 3.0
    gun_pose_score = _clamp(
        0.45 * support_finger_curl + 0.35 * thumb_extension + 0.20 * index_extension
    )
    is_gun_pose = gun_pose_score >= 0.55 and support_finger_curl >= 0.45

    finger_count = sum(
        1
        for value in (index_extension, middle_extension, ring_extension, pinky_extension)
        if value >= 0.45
    )
    wrist = landmarks[WRIST]
    index_mcp = landmarks[INDEX_MCP]
    index_pip = landmarks[INDEX_PIP]
    direction_x = index_pip.x - index_mcp.x
    direction_y = index_pip.y - index_mcp.y
    aim_target = (
        _clamp(wrist.x + (direction_x * 2.2)),
        _clamp(wrist.y + (direction_y * 2.2)),
    )
    palm_center = (
        (landmarks[WRIST].x + landmarks[INDEX_MCP].x + landmarks[PINKY_MCP].x) / 3.0,
        (landmarks[WRIST].y + landmarks[INDEX_MCP].y + landmarks[PINKY_MCP].y) / 3.0,
    )

    return HandFeatures(
        label=observation.label,
        confidence=_clamp(observation.score),
        gun_pose_score=gun_pose_score,
        is_gun_pose=is_gun_pose,
        trigger_curl=_clamp(1.0 - index_extension),
        finger_count=finger_count,
        aim_origin=(wrist.x, wrist.y),
        aim_target=aim_target,
        palm_center=palm_center,
    )


def extract_frame_features(
    observations: list[HandObservation],
    timestamp_seconds: float,
) -> FrameFeatures:
    return FrameFeatures(
        timestamp_seconds=timestamp_seconds,
        hands=tuple(extract_hand_features(observation) for observation in observations),
    )
