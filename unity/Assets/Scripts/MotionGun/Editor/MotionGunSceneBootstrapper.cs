#if UNITY_EDITOR
using MotionGun.Gameplay;
using MotionGun.Runtime;
using MotionGun.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MotionGun.Editor
{
    public static class MotionGunSceneBootstrapper
    {
        [MenuItem("MotionGun/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 1.6f, -6f);
            camera.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            UdpGestureClient gestureClient = camera.gameObject.GetComponent<UdpGestureClient>();
            if (gestureClient == null)
            {
                gestureClient = camera.gameObject.AddComponent<UdpGestureClient>();
            }

            CreateGround();
            CreateBackdrop();

            GameObject controllerObject = new GameObject("MotionGunController");
            MotionGunController controller = controllerObject.AddComponent<MotionGunController>();
            RangeSessionController sessionController = controllerObject.AddComponent<RangeSessionController>();

            GameObject pivotObject = new GameObject("WeaponPivot");
            pivotObject.transform.SetParent(controllerObject.transform, false);
            pivotObject.transform.position = camera.transform.position + (camera.transform.forward * 0.75f);
            GameObject weaponVisualsRoot = new GameObject("WeaponVisuals");
            weaponVisualsRoot.transform.SetParent(pivotObject.transform, false);

            LineRenderer tracer = controllerObject.AddComponent<LineRenderer>();
            tracer.enabled = false;
            tracer.startWidth = 0.02f;
            tracer.endWidth = 0.006f;
            tracer.positionCount = 2;
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                tracer.material = new Material(lineShader);
            }
            tracer.startColor = new Color(1f, 0.95f, 0.6f, 1f);
            tracer.endColor = new Color(1f, 0.4f, 0.1f, 0.5f);

            Canvas canvas = CreateCanvas();
            RangeHudController hud = canvas.gameObject.AddComponent<RangeHudController>();
            TMP_Text weaponLabel = CreateText(canvas.transform, "WeaponLabel", new Vector2(24f, -24f), 30f, "PISTOL", TextAlignmentOptions.TopLeft);
            TMP_Text ammoLabel = CreateText(canvas.transform, "AmmoLabel", new Vector2(24f, -60f), 26f, "12 / 12", TextAlignmentOptions.TopLeft);
            TMP_Text statusLabel = CreateText(canvas.transform, "StatusLabel", new Vector2(24f, -96f), 24f, "READY", TextAlignmentOptions.TopLeft);
            TMP_Text confidenceLabel = CreateText(canvas.transform, "ConfidenceLabel", new Vector2(24f, -128f), 22f, "TRACK 0.00", TextAlignmentOptions.TopLeft);
            TMP_Text scoreLabel = CreateText(canvas.transform, "ScoreLabel", new Vector2(24f, -164f), 22f, "SCORE 0  HIT 0/0  0%", TextAlignmentOptions.TopLeft);
            TMP_Text eventLabel = CreateText(canvas.transform, "EventLabel", new Vector2(24f, -210f), 24f, "START PYTHON SENDER", TextAlignmentOptions.TopLeft);
            TMP_Text waveLabel = CreateText(canvas.transform, "WaveLabel", new Vector2(-24f, -24f), 26f, "WAVE 0/4", TextAlignmentOptions.TopRight);
            TMP_Text timerLabel = CreateText(canvas.transform, "TimerLabel", new Vector2(-24f, -60f), 28f, "TIME 01:30", TextAlignmentOptions.TopRight);
            TMP_Text remainingTargetsLabel = CreateText(canvas.transform, "TargetsLabel", new Vector2(-24f, -96f), 24f, "TARGETS 0", TextAlignmentOptions.TopRight);
            TMP_Text bannerLabel = CreateCenteredText(canvas.transform, "BannerLabel", new Vector2(0f, -72f), 44f, "FIRE TO START");
            eventLabel.color = new Color(1f, 0.9f, 0.25f, 1f);
            bannerLabel.color = new Color(1f, 0.95f, 0.55f, 1f);
            AimReticleController reticle = CreateReticle(canvas.transform);

            ConfigureHud(
                hud,
                weaponLabel,
                ammoLabel,
                statusLabel,
                confidenceLabel,
                scoreLabel,
                eventLabel,
                waveLabel,
                timerLabel,
                remainingTargetsLabel,
                bannerLabel
            );
            ConfigureController(controller, gestureClient, camera, pivotObject.transform, tracer, hud, reticle);

            GameObject targetsRoot = new GameObject("Targets");
            RangeTarget[] targets = new[]
            {
                CreateTarget(targetsRoot.transform, "Target01", new Vector3(0f, 1.5f, 10f), Vector3.right),
                CreateTarget(targetsRoot.transform, "Target02", new Vector3(-2.5f, 1.4f, 14f), Vector3.right),
                CreateTarget(targetsRoot.transform, "Target03", new Vector3(2.8f, 1.8f, 18f), Vector3.left),
                CreateTarget(targetsRoot.transform, "Target04", new Vector3(-4.4f, 1.6f, 20f), Vector3.left),
                CreateTarget(targetsRoot.transform, "Target05", new Vector3(4.2f, 1.35f, 22f), Vector3.right),
                CreateTarget(targetsRoot.transform, "Target06", new Vector3(0.75f, 2.05f, 16f), Vector3.left),
            };
            ConfigureSessionController(sessionController, controller, targets);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = controllerObject;
        }

        private static void ConfigureController(
            MotionGunController controller,
            UdpGestureClient gestureClient,
            Camera camera,
            Transform weaponPivot,
            LineRenderer tracer,
            RangeHudController hud,
            AimReticleController reticle
        )
        {
            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("gestureClient").objectReferenceValue = gestureClient;
            serializedObject.FindProperty("aimCamera").objectReferenceValue = camera;
            serializedObject.FindProperty("weaponPivot").objectReferenceValue = weaponPivot;
            serializedObject.FindProperty("tracer").objectReferenceValue = tracer;
            serializedObject.FindProperty("hud").objectReferenceValue = hud;
            serializedObject.FindProperty("reticle").objectReferenceValue = reticle;
            serializedObject.FindProperty("minTrackingConfidence").floatValue = 0.45f;
            serializedObject.FindProperty("maxPacketAgeSeconds").floatValue = 0.35f;

            SerializedProperty weapons = serializedObject.FindProperty("weapons");
            weapons.arraySize = 3;
            Transform pistolVisual = CreateWeaponVisual(weaponPivot, weaponVisualsRoot.transform, "PistolVisual", new Color(0.22f, 0.24f, 0.28f, 1f), new Vector3(0.14f, -0.12f, 0.32f), new Vector3(0.04f, -0.24f, 0.1f), new Vector3(0f, 0f, 0.5f), false);
            Transform burstVisual = CreateWeaponVisual(weaponPivot, weaponVisualsRoot.transform, "BurstVisual", new Color(0.16f, 0.32f, 0.4f, 1f), new Vector3(0.17f, -0.11f, 0.4f), new Vector3(0.05f, -0.24f, 0.11f), new Vector3(0f, 0f, 0.58f), true);
            Transform handCannonVisual = CreateWeaponVisual(weaponPivot, weaponVisualsRoot.transform, "HandCannonVisual", new Color(0.32f, 0.18f, 0.16f, 1f), new Vector3(0.18f, -0.1f, 0.28f), new Vector3(0.055f, -0.24f, 0.1f), new Vector3(0f, 0f, 0.46f), false);
            SetWeapon(weapons.GetArrayElementAtIndex(0), 1, "Pistol", 12, 0.18f, 1.1f, 1f, 1, 0.07f, pistolVisual, 0.06f, 9f);
            SetWeapon(weapons.GetArrayElementAtIndex(1), 2, "Burst", 18, 0.22f, 1.35f, 0.8f, 3, 0.06f, burstVisual, 0.045f, 12f);
            SetWeapon(weapons.GetArrayElementAtIndex(2), 3, "Hand Cannon", 6, 0.45f, 1.6f, 2.4f, 1, 0.07f, handCannonVisual, 0.1f, 7f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSessionController(
            RangeSessionController sessionController,
            MotionGunController motionGunController,
            RangeTarget[] targets
        )
        {
            SerializedObject serializedObject = new SerializedObject(sessionController);
            serializedObject.FindProperty("motionGunController").objectReferenceValue = motionGunController;
            serializedObject.FindProperty("sessionDurationSeconds").floatValue = 90f;
            serializedObject.FindProperty("waveIntroDuration").floatValue = 1.25f;

            SerializedProperty targetPool = serializedObject.FindProperty("targetPool");
            targetPool.arraySize = targets.Length;
            for (int index = 0; index < targets.Length; index++)
            {
                targetPool.GetArrayElementAtIndex(index).objectReferenceValue = targets[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureHud(
            RangeHudController hud,
            TMP_Text weaponLabel,
            TMP_Text ammoLabel,
            TMP_Text statusLabel,
            TMP_Text confidenceLabel,
            TMP_Text scoreLabel,
            TMP_Text eventLabel,
            TMP_Text waveLabel,
            TMP_Text timerLabel,
            TMP_Text remainingTargetsLabel,
            TMP_Text bannerLabel
        )
        {
            SerializedObject serializedObject = new SerializedObject(hud);
            serializedObject.FindProperty("weaponLabel").objectReferenceValue = weaponLabel;
            serializedObject.FindProperty("ammoLabel").objectReferenceValue = ammoLabel;
            serializedObject.FindProperty("statusLabel").objectReferenceValue = statusLabel;
            serializedObject.FindProperty("confidenceLabel").objectReferenceValue = confidenceLabel;
            serializedObject.FindProperty("scoreLabel").objectReferenceValue = scoreLabel;
            serializedObject.FindProperty("eventLabel").objectReferenceValue = eventLabel;
            serializedObject.FindProperty("waveLabel").objectReferenceValue = waveLabel;
            serializedObject.FindProperty("timerLabel").objectReferenceValue = timerLabel;
            serializedObject.FindProperty("remainingTargetsLabel").objectReferenceValue = remainingTargetsLabel;
            serializedObject.FindProperty("bannerLabel").objectReferenceValue = bannerLabel;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetWeapon(
            SerializedProperty property,
            int slotId,
            string displayName,
            int magazineSize,
            float fireInterval,
            float reloadDuration,
            float damage,
            int shotsPerTrigger,
            float burstInterval,
            Transform weaponVisualRoot,
            float recoilDistance,
            float recoilRecoverSpeed
        )
        {
            property.FindPropertyRelative("SlotId").intValue = slotId;
            property.FindPropertyRelative("DisplayName").stringValue = displayName;
            property.FindPropertyRelative("MagazineSize").intValue = magazineSize;
            property.FindPropertyRelative("FireInterval").floatValue = fireInterval;
            property.FindPropertyRelative("ReloadDuration").floatValue = reloadDuration;
            property.FindPropertyRelative("Damage").floatValue = damage;
            property.FindPropertyRelative("ShotsPerTrigger").intValue = shotsPerTrigger;
            property.FindPropertyRelative("BurstInterval").floatValue = burstInterval;
            property.FindPropertyRelative("WeaponVisualRoot").objectReferenceValue = weaponVisualRoot;
            property.FindPropertyRelative("RecoilDistance").floatValue = recoilDistance;
            property.FindPropertyRelative("RecoilRecoverSpeed").floatValue = recoilRecoverSpeed;
        }

        private static Transform CreateWeaponVisual(
            Transform weaponPivot,
            Transform parent,
            string name,
            Color color,
            Vector3 bodyScale,
            Vector3 gripLocalPosition,
            Vector3 barrelLocalPosition,
            bool addTopRail
        )
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0.22f, -0.18f, 0.42f);
            root.transform.localRotation = Quaternion.identity;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = bodyScale;
            body.transform.localPosition = Vector3.zero;
            ApplySharedColor(body, color);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            barrel.transform.SetParent(root.transform, false);
            barrel.transform.localScale = new Vector3(bodyScale.x * 0.38f, bodyScale.y * 0.55f, bodyScale.z * 0.78f);
            barrel.transform.localPosition = barrelLocalPosition;
            ApplySharedColor(barrel, Color.Lerp(color, Color.black, 0.2f));
            Object.DestroyImmediate(barrel.GetComponent<Collider>());

            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(root.transform, false);
            grip.transform.localScale = new Vector3(bodyScale.x * 0.28f, bodyScale.y * 1.15f, bodyScale.x * 0.6f);
            grip.transform.localPosition = gripLocalPosition;
            grip.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            ApplySharedColor(grip, Color.Lerp(color, Color.black, 0.32f));
            Object.DestroyImmediate(grip.GetComponent<Collider>());

            if (addTopRail)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "TopRail";
                rail.transform.SetParent(root.transform, false);
                rail.transform.localScale = new Vector3(bodyScale.x * 0.72f, bodyScale.y * 0.18f, bodyScale.z * 0.44f);
                rail.transform.localPosition = new Vector3(0f, bodyScale.y * 0.62f, bodyScale.z * 0.06f);
                ApplySharedColor(rail, Color.Lerp(color, Color.white, 0.1f));
                Object.DestroyImmediate(rail.GetComponent<Collider>());
            }

            GameObject sight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sight.name = "Sight";
            sight.transform.SetParent(root.transform, false);
            sight.transform.localScale = new Vector3(bodyScale.x * 0.08f, bodyScale.y * 0.16f, bodyScale.x * 0.08f);
            sight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            sight.transform.localPosition = new Vector3(0f, bodyScale.y * 0.62f, bodyScale.z * 0.3f);
            ApplySharedColor(sight, new Color(0.08f, 0.08f, 0.08f, 1f));
            Object.DestroyImmediate(sight.GetComponent<Collider>());

            root.gameObject.SetActive(false);
            return root.transform;
        }

        private static void ApplySharedColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = color;
            }
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            float fontSize,
            string text,
            TextAlignmentOptions alignment
        )
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.pivot = alignment == TextAlignmentOptions.TopRight
                ? new Vector2(1f, 1f)
                : new Vector2(0f, 1f);
            rectTransform.anchorMin = alignment == TextAlignmentOptions.TopRight
                ? new Vector2(1f, 1f)
                : new Vector2(0f, 1f);
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(680f, 40f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.text = text;
            label.alignment = alignment;
            label.color = Color.white;
            return label;
        }

        private static TMP_Text CreateCenteredText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            float fontSize,
            string text
        )
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(900f, 56f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.text = text;
            label.alignment = TextAlignmentOptions.Top;
            label.color = Color.white;
            return label;
        }

        private static AimReticleController CreateReticle(Transform parent)
        {
            GameObject reticleObject = new GameObject("Reticle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(AimReticleController));
            reticleObject.transform.SetParent(parent, false);

            RectTransform rectTransform = reticleObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(60f, 60f);
            rectTransform.anchoredPosition = Vector2.zero;

            TextMeshProUGUI glyph = reticleObject.GetComponent<TextMeshProUGUI>();
            glyph.text = "+";
            glyph.fontSize = 42f;
            glyph.alignment = TextAlignmentOptions.Center;
            glyph.color = new Color(1f, 0.9f, 0.25f, 1f);

            AimReticleController controller = reticleObject.GetComponent<AimReticleController>();
            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("reticle").objectReferenceValue = rectTransform;
            serializedObject.FindProperty("canvasRect").objectReferenceValue = parent as RectTransform;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = new Color(0.18f, 0.2f, 0.23f, 1f);
            }
        }

        private static void CreateBackdrop()
        {
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "Backdrop";
            backdrop.transform.position = new Vector3(0f, 2.5f, 24f);
            backdrop.transform.localScale = new Vector3(18f, 6f, 0.5f);
            Renderer renderer = backdrop.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            }
        }

        private static RangeTarget CreateTarget(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 travelAxis
        )
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            target.name = name;
            target.transform.SetParent(parent, false);
            target.transform.position = position;
            target.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);

            RangeTarget rangeTarget = target.AddComponent<RangeTarget>();
            SerializedObject serializedObject = new SerializedObject(rangeTarget);
            serializedObject.FindProperty("travelAxis").vector3Value = travelAxis;
            serializedObject.FindProperty("travelDistance").floatValue = 1.6f;
            serializedObject.FindProperty("travelSpeed").floatValue = 1.2f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return rangeTarget;
        }
    }
}
#endif
