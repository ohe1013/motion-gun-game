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

            GameObject pivotObject = new GameObject("WeaponPivot");
            pivotObject.transform.SetParent(controllerObject.transform, false);
            pivotObject.transform.position = camera.transform.position + (camera.transform.forward * 0.75f);

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
            AimReticleController reticle = CreateReticle(canvas.transform);

            ConfigureHud(hud, weaponLabel, ammoLabel, statusLabel, confidenceLabel);
            ConfigureController(controller, gestureClient, camera, pivotObject.transform, tracer, hud, reticle);

            CreateTarget(new Vector3(0f, 1.5f, 10f), false);
            CreateTarget(new Vector3(-2.5f, 1.4f, 14f), true);
            CreateTarget(new Vector3(2.8f, 1.8f, 18f), true);

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

            SerializedProperty weapons = serializedObject.FindProperty("weapons");
            weapons.arraySize = 3;
            SetWeapon(weapons.GetArrayElementAtIndex(0), 1, "Pistol", 12, 0.18f, 1.1f, 1f);
            SetWeapon(weapons.GetArrayElementAtIndex(1), 2, "Burst", 18, 0.11f, 1.35f, 0.8f);
            SetWeapon(weapons.GetArrayElementAtIndex(2), 3, "Hand Cannon", 6, 0.45f, 1.6f, 2.4f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureHud(
            RangeHudController hud,
            TMP_Text weaponLabel,
            TMP_Text ammoLabel,
            TMP_Text statusLabel,
            TMP_Text confidenceLabel
        )
        {
            SerializedObject serializedObject = new SerializedObject(hud);
            serializedObject.FindProperty("weaponLabel").objectReferenceValue = weaponLabel;
            serializedObject.FindProperty("ammoLabel").objectReferenceValue = ammoLabel;
            serializedObject.FindProperty("statusLabel").objectReferenceValue = statusLabel;
            serializedObject.FindProperty("confidenceLabel").objectReferenceValue = confidenceLabel;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetWeapon(
            SerializedProperty property,
            int slotId,
            string displayName,
            int magazineSize,
            float fireInterval,
            float reloadDuration,
            float damage
        )
        {
            property.FindPropertyRelative("SlotId").intValue = slotId;
            property.FindPropertyRelative("DisplayName").stringValue = displayName;
            property.FindPropertyRelative("MagazineSize").intValue = magazineSize;
            property.FindPropertyRelative("FireInterval").floatValue = fireInterval;
            property.FindPropertyRelative("ReloadDuration").floatValue = reloadDuration;
            property.FindPropertyRelative("Damage").floatValue = damage;
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
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(520f, 40f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.text = text;
            label.alignment = alignment;
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

        private static void CreateTarget(Vector3 position, bool moving)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            target.name = moving ? "MovingTarget" : "StaticTarget";
            target.transform.position = position;
            target.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);

            RangeTarget rangeTarget = target.AddComponent<RangeTarget>();
            if (moving)
            {
                SerializedObject serializedObject = new SerializedObject(rangeTarget);
                serializedObject.FindProperty("travelAxis").vector3Value = Vector3.right;
                serializedObject.FindProperty("travelDistance").floatValue = 1.6f;
                serializedObject.FindProperty("travelSpeed").floatValue = 1.35f;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
