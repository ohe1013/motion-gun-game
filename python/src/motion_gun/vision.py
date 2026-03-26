from __future__ import annotations

import shutil
from pathlib import Path
from urllib.request import urlopen

from .models import HandObservation, Landmark

try:
    import cv2
    import mediapipe as mp
except ImportError:  # pragma: no cover - exercised only when runtime deps are missing
    cv2 = None
    mp = None

MODEL_URL = (
    "https://storage.googleapis.com/mediapipe-models/hand_landmarker/"
    "hand_landmarker/float16/latest/hand_landmarker.task"
)


def _default_model_path() -> Path:
    return Path(__file__).resolve().parents[2] / "models" / "hand_landmarker.task"


def _ensure_model_asset(model_path: Path) -> Path:
    if model_path.exists():
        return model_path

    model_path.parent.mkdir(parents=True, exist_ok=True)
    with urlopen(MODEL_URL, timeout=60) as response, model_path.open("wb") as output:
        shutil.copyfileobj(response, output)
    return model_path


class MediaPipeHandTracker:
    def __init__(
        self,
        *,
        max_num_hands: int = 2,
        min_detection_confidence: float = 0.6,
        min_tracking_confidence: float = 0.5,
        model_complexity: int = 1,
        model_path: str | None = None,
    ) -> None:
        if cv2 is None or mp is None:
            raise RuntimeError(
                "MediaPipe runtime dependencies are missing. Install requirements.txt first."
            )

        self._mode = "solutions" if hasattr(mp, "solutions") else "tasks"
        self._hands = None
        self._landmarker = None

        if self._mode == "solutions":
            self._hands = mp.solutions.hands.Hands(
                static_image_mode=False,
                max_num_hands=max_num_hands,
                model_complexity=model_complexity,
                min_detection_confidence=min_detection_confidence,
                min_tracking_confidence=min_tracking_confidence,
            )
            return

        resolved_model_path = _ensure_model_asset(
            Path(model_path) if model_path is not None else _default_model_path()
        )
        options = mp.tasks.vision.HandLandmarkerOptions(
            base_options=mp.tasks.BaseOptions(model_asset_path=str(resolved_model_path)),
            running_mode=mp.tasks.vision.RunningMode.VIDEO,
            num_hands=max_num_hands,
            min_hand_detection_confidence=min_detection_confidence,
            min_hand_presence_confidence=min_tracking_confidence,
            min_tracking_confidence=min_tracking_confidence,
        )
        self._landmarker = mp.tasks.vision.HandLandmarker.create_from_options(options)

    def detect(self, frame_bgr, timestamp_ms: int | None = None) -> list[HandObservation]:
        if self._mode == "solutions":
            return self._detect_with_solutions(frame_bgr)
        return self._detect_with_tasks(frame_bgr, timestamp_ms)

    def _detect_with_solutions(self, frame_bgr) -> list[HandObservation]:
        rgb_frame = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        results = self._hands.process(rgb_frame)
        observations: list[HandObservation] = []

        if not results.multi_hand_landmarks or not results.multi_handedness:
            return observations

        for handedness, landmark_list in zip(
            results.multi_handedness,
            results.multi_hand_landmarks,
            strict=False,
        ):
            classification = handedness.classification[0]
            observations.append(
                HandObservation(
                    label=classification.label,
                    score=classification.score,
                    landmarks=tuple(
                        Landmark(landmark.x, landmark.y, landmark.z)
                        for landmark in landmark_list.landmark
                    ),
                )
            )

        return observations

    def _detect_with_tasks(
        self,
        frame_bgr,
        timestamp_ms: int | None,
    ) -> list[HandObservation]:
        if timestamp_ms is None:
            raise ValueError("timestamp_ms is required when using the MediaPipe tasks API.")

        rgb_frame = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)
        results = self._landmarker.detect_for_video(mp_image, timestamp_ms)
        observations: list[HandObservation] = []

        if not results.hand_landmarks or not results.handedness:
            return observations

        for handedness_list, landmark_list in zip(
            results.handedness,
            results.hand_landmarks,
            strict=False,
        ):
            handedness = handedness_list[0]
            observations.append(
                HandObservation(
                    label=handedness.category_name or handedness.display_name or "Unknown",
                    score=handedness.score or 0.0,
                    landmarks=tuple(
                        Landmark(landmark.x, landmark.y, landmark.z)
                        for landmark in landmark_list
                    ),
                )
            )

        return observations

    def close(self) -> None:
        if self._hands is not None:
            self._hands.close()
        if self._landmarker is not None:
            self._landmarker.close()
