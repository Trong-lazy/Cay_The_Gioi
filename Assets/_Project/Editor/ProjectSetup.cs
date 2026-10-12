using System;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Configuration;
using CayTheGioi.Domain;
using CayTheGioi.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CayTheGioi.Editor
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/MainScene.unity";
        public const string ConfigPath = "Assets/_Project/Configs/PrototypeConfig.asset";
        public static readonly string[] Folders =
        {
            "Assets/_Project/Scenes", "Assets/_Project/Scripts/Domain", "Assets/_Project/Scripts/Configuration",
            "Assets/_Project/Scripts/Bootstrap", "Assets/_Project/Scripts/Presentation", "Assets/_Project/Scripts/Infrastructure",
            "Assets/_Project/Editor", "Assets/_Project/Configs", "Assets/_Project/Prefabs/Trees",
            "Assets/_Project/UI", "Assets/_Project/Art/Models", "Assets/_Project/Art/Materials",
            "Assets/_Project/Art/Textures/Tree_Common", "Assets/_Project/Art/Placeholders/Materials", "Assets/_Project/Audio",
            "Assets/_Project/Tests/EditMode", "Assets/_Project/Tests/PlayMode"
        };

        [MenuItem("Cây Thế Giới/Setup/Prepare Project")]
        public static void PrepareProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before setup.");
            // Never discard unsaved scene edits during setup.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before setup.");
            foreach (var folder in Folders) EnsureFolder(folder);
            ApplySettings();
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                SetStartupScene();
                Debug.Log("[CayTheGioi] Existing MainScene preserved. Project settings and folders verified.");
                return;
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("The project's URP Lit shader is unavailable.");
            var soil = Material("Soil", shader, new Color(0.28f, 0.20f, 0.13f));
            var bark = Material("Bark", shader, new Color(0.30f, 0.23f, 0.12f));
            var leaf = Material("Leaf", shader, new Color(0.18f, 0.53f, 0.23f));
            var visuals = new[]
            {
                new StageVisualDefinition { stageId = "seedling", prefab = Placeholder("seedling", bark, leaf, 0), cameraDistance = 1.4f, cameraPitch = 16f, cameraTargetHeight = 0.12f },
                new StageVisualDefinition { stageId = "young_tree", prefab = Placeholder("young_tree", bark, leaf, 1), cameraDistance = 2.8f, cameraPitch = 18f, cameraTargetHeight = 0.42f },
                new StageVisualDefinition { stageId = "large_tree", prefab = Placeholder("large_tree", bark, leaf, 2), cameraDistance = 6.2f, cameraPitch = 20f, cameraTargetHeight = 1.18f }
            };
            var config = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PrototypeConfig>();
                config.ConfigureDefaults(visuals);
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            var errors = config.Validate();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var world = new GameObject("World");
            var ground = Primitive("Ground", PrimitiveType.Cylinder, world.transform, new Vector3(0, -0.12f, 0), new Vector3(5.8f, 0.12f, 5.8f), soil, true);
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<CapsuleCollider>());
            var groundBox = ground.AddComponent<BoxCollider>();
            groundBox.size = ground.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            groundBox.center = Vector3.zero;
            var lightObject = new GameObject("Directional Light", typeof(Light), typeof(UniversalAdditionalLightData));
            lightObject.transform.SetParent(world.transform);
            lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.79f, 0.78f);
            RenderSettings.sun = light;

            var treeObject = new GameObject("Tree", typeof(TreeView), typeof(SphereCollider));
            treeObject.transform.SetParent(world.transform);
            var slot = Child("ModelSlot", treeObject.transform, Vector3.zero);
            var rootAnchor = Child("RootAnchor", treeObject.transform, new Vector3(0, 0.05f, 0));
            var leavesAnchor = Child("LeavesAnchor", treeObject.transform, new Vector3(0, 0.98f, 0));
            var trunkAnchor = Child("TrunkAnchor", treeObject.transform, new Vector3(0, 0.40f, 0));
            var model = (GameObject)PrefabUtility.InstantiatePrefab(visuals[0].prefab, slot);
            model.name = "StageModel";
            var treeView = treeObject.GetComponent<TreeView>();
            treeView.Configure(slot, rootAnchor, leavesAnchor, trunkAnchor, model, "seedling");
            var clickCollider = treeObject.GetComponent<SphereCollider>();
            clickCollider.center = new Vector3(0, 0.60f, 0);
            clickCollider.radius = 0.65f;

            var cameras = new GameObject("CameraRig", typeof(OrbitCameraRig));
            var pivot = Child("CameraPivot", cameras.transform, new Vector3(0, 0.65f, 0));
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameras.transform);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.84f, 0.91f, 0.87f);
            camera.fieldOfView = 38;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100;
            var orbit = cameras.GetComponent<OrbitCameraRig>();
            orbit.Configure(pivot, camera);
            orbit.SetView(visuals[0].cameraDistance, visuals[0].cameraPitch);

            var canvasObject = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FoundationHudView));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Label("Title", canvas.transform, font, "CÂY THẾ GIỚI", new Vector2(0, 1), new Vector2(42, -30), new Vector2(700, 50), 32, TextAnchor.UpperLeft);
            var resourceText = Label("Resources", canvas.transform, font, "", new Vector2(0, 1), new Vector2(42, -86), new Vector2(790, 105), 24, TextAnchor.UpperLeft);
            var stageText = Label("Stage", canvas.transform, font, "Mầm", new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(500, 55), 30, TextAnchor.UpperCenter);
            var controls = Label("Controls", canvas.transform, font, "Giữ chuột phải và kéo để xoay quanh cây", new Vector2(0.5f, 0), new Vector2(0, 38), new Vector2(1000, 55), 23, TextAnchor.MiddleCenter);
            controls.color = new Color(0.94f, 0.97f, 0.92f);
            var hud = canvasObject.GetComponent<FoundationHudView>();
            hud.Configure(resourceText, stageText);
            hud.Bind(GameStateFactory.CreateNew(config.Rules), "Mầm", config.Rules.tapsPerReward);
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var bootstrap = new GameObject("GameBootstrapper", typeof(GameBootstrapper)).GetComponent<GameBootstrapper>();
            bootstrap.Configure(config, treeView, orbit, hud);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            SetStartupScene();
            Selection.activeGameObject = treeObject;
            Debug.Log("[CayTheGioi] T00 setup complete: " + ScenePath + ". Gameplay systems begin in T01.");
        }

        private static void ApplySettings()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
            EditorSettings.projectGenerationRootNamespace = "CayTheGioi";
            // Keep the original product/company names so persistentDataPath stays stable.
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if (pipeline == null) throw new InvalidOperationException("The existing PC URP pipeline is missing.");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.names[i] == "PC") QualitySettings.SetQualityLevel(i);
        }

        private static void SetStartupScene()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            // A forced Play start scene would replace the test runner's temporary scene.
            // MainScene remains the build entry and the currently open working scene.
            EditorSceneManager.playModeStartScene = null;
        }

        [MenuItem("Cây Thế Giới/Setup/Open MainScene")]
        public static void OpenMainScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Material Material(string name, Shader shader, Color color)
        {
            string path = "Assets/_Project/Art/Placeholders/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Placeholder(string id, Material bark, Material leaf, int stage)
        {
            string path = "Assets/_Project/Prefabs/Trees/" + id + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var model = new GameObject(id);
            float height = stage == 0 ? 0.82f : stage == 1 ? 1.65f : 2.8f;
            float width = stage == 0 ? 0.10f : stage == 1 ? 0.26f : 0.48f;
            Primitive("Stem", PrimitiveType.Cylinder, model.transform, new Vector3(0, height / 2, 0), new Vector3(width, height / 2, width), bark, false);
            if (stage == 0)
            {
                var left = Primitive("LeafLeft", PrimitiveType.Sphere, model.transform, new Vector3(-0.22f, 0.88f, 0), new Vector3(0.62f, 0.18f, 0.30f), leaf, false);
                left.transform.localRotation = Quaternion.Euler(0, 0, -25);
                var right = Primitive("LeafRight", PrimitiveType.Sphere, model.transform, new Vector3(0.22f, 0.98f, 0), new Vector3(0.62f, 0.18f, 0.30f), leaf, false);
                right.transform.localRotation = Quaternion.Euler(0, 0, 25);
            }
            else
            {
                float scale = stage == 1 ? 1.25f : 2.1f;
                Primitive("Crown", PrimitiveType.Sphere, model.transform, new Vector3(0, height, 0), Vector3.one * scale, leaf, false);
                Primitive("CrownLeft", PrimitiveType.Sphere, model.transform, new Vector3(-scale * 0.35f, height * 0.85f, 0), Vector3.one * scale * 0.8f, leaf, false);
                Primitive("CrownRight", PrimitiveType.Sphere, model.transform, new Vector3(scale * 0.35f, height * 0.9f, 0.12f), Vector3.one * scale * 0.8f, leaf, false);
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(model, path);
            UnityEngine.Object.DestroyImmediate(model);
            return prefab;
        }

        private static Transform Child(string name, Transform parent, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        private static Text Label(string name, Transform parent, Font font, string content, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.08f, 0.23f, 0.15f);
            text.raycastTarget = false;
            text.text = content;
            return text;
        }
    }
}
