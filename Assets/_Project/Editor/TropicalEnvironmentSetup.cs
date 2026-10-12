using System.IO;
using CayTheGioi.MapEnvironment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CayTheGioi.Editor
{
    public static class TropicalEnvironmentSetup
    {
        private const string MatDir = "Assets/_Project/Art/Environment/Materials";
        private const string MeshDir = "Assets/_Project/Art/Environment/Meshes";
        private const string PrefabDir = "Assets/_Project/Art/Environment/Prefabs";

        [MenuItem("Cây Thế Giới/Setup Tropical Environment")]
        public static void SetupEnvironment()
        {
            EnsureDirectories();

            // 1. Khởi tạo và lưu các Material URP Lit với Cull Back cho khối kín, Cull Off cho lá/hoa
            var matGrass = GetOrCreateMaterial("M_Tropical_Grass", new Color(0.30f, 0.66f, 0.20f), 0.22f, 0f, 2);
            var matWarmDirt = GetOrCreateMaterial("M_Warm_Dirt", new Color(0.52f, 0.35f, 0.20f), 0.18f, 0f, 2);
            var matSandyShore = GetOrCreateMaterial("M_Sandy_Shore", new Color(0.92f, 0.84f, 0.60f), 0.26f, 0f, 2);
            var matLakeBed = GetOrCreateMaterial("M_Lake_Bed", new Color(0.24f, 0.32f, 0.30f), 0.25f, 0f, 2);

            var matLakeWater = GetOrCreateTransparentMaterial("M_Lake_Water", new Color(0.14f, 0.90f, 0.82f, 0.72f), 0.96f, 0.05f, 2);
            var matWaterfall = GetOrCreateTransparentMaterial("M_Waterfall", new Color(0.72f, 1.0f, 0.95f, 0.85f), 0.90f, 0f, 0);

            var matMossyRock = GetOrCreateMaterial("M_Mossy_Rock", new Color(0.28f, 0.34f, 0.30f), 0.30f, 0.05f, 2);
            var matTropicalFoliage = GetOrCreateMaterial("M_Tropical_Foliage", new Color(0.18f, 0.62f, 0.24f), 0.35f, 0f, 0); // Cull Off cho lá
            var matCoralFlower = GetOrCreateMaterial("M_Coral_Flower", new Color(1.0f, 0.38f, 0.42f), 0.25f, 0f, 0);    // Cull Off cho hoa
            var matMushroomStem = GetOrCreateMaterial("M_Glow_Mushroom_Stem", new Color(0.90f, 0.98f, 0.96f), 0.35f, 0f, 2);
            var matMushroomCap = GetOrCreateEmissiveMaterial("M_Glow_Mushroom_Cap", new Color(0.08f, 0.92f, 0.95f), new Color(0.15f, 0.95f, 1.0f) * 3.5f, 0.75f, 2);
            var matPalmTrunk = GetOrCreateMaterial("M_Palm_Trunk", new Color(0.44f, 0.30f, 0.20f), 0.22f, 0f, 2);
            var matFireflies = GetOrCreateFireflyMaterial();

            // Giữ nguyên vật liệu cây theo yêu cầu nghiệp vụ
            // PolishTreeLeafMaterials();

            AssetDatabase.SaveAssets();

            // 2. Tìm hoặc tạo GameObject WorldEnvironment trong MainScene
            var world = GameObject.Find("World");
            if (world == null)
            {
                Debug.LogError("World root GameObject not found in scene!");
                return;
            }

            // Xử lý đĩa Ground cũ: Tắt MeshRenderer và Collider để không che lấp nền cầu
            var oldGround = GameObject.Find("World/Ground");
            if (oldGround != null)
            {
                var oldMr = oldGround.GetComponent<MeshRenderer>();
                if (oldMr != null) oldMr.enabled = false;
                var oldCol = oldGround.GetComponent<BoxCollider>();
                if (oldCol != null) oldCol.enabled = false;
            }

            var envTrans = world.transform.Find("WorldEnvironment");
            GameObject envGo;
            if (envTrans == null)
            {
                envGo = new GameObject("WorldEnvironment");
                envGo.transform.SetParent(world.transform, false);
            }
            else
            {
                envGo = envTrans.gameObject;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(envGo))
                PrefabUtility.UnpackPrefabInstance(envGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var controller = envGo.GetComponent<WorldEnvironmentController>();
            if (controller == null)
            {
                controller = envGo.AddComponent<WorldEnvironmentController>();
            }

            controller.BindMaterials(
                matGrass, matWarmDirt, matSandyShore, matLakeBed,
                matLakeWater, matWaterfall, matMossyRock, matTropicalFoliage,
                matCoralFlower, matMushroomStem, matMushroomCap, matPalmTrunk, matFireflies
            );

            // Dựng toàn bộ môi trường
            controller.BuildEnvironment();

            // 3. Thiết lập URP Post-Processing Volume & Lighting
            SetupPostProcessingAndLighting(envGo);

            // 4. Lưu toàn bộ các Mesh in-memory thành file Asset vĩnh viễn
            SaveAllMeshesAsAssets(envGo);

            // 5. Lưu thành Prefab để tái sử dụng và đảm bảo không mất dữ liệu
            string prefabPath = $"{PrefabDir}/WorldEnvironment.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(envGo, prefabPath, InteractionMode.AutomatedAction);

            // Đánh dấu dirty và lưu scene
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(envGo);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[TropicalEnvironmentSetup] Tropical Fantasy environment polished, Post-Processing configured, serialized to assets/prefab, and saved to MainScene successfully!");
        }

        private static void PolishTreeLeafMaterials()
        {
            string[] leafMatPaths = new string[]
            {
                "Assets/_Project/Art/Materials/M_Seedling_Leaves.mat",
                "Assets/_Project/Art/Materials/M_Young_Leaves.mat",
                "Assets/_Project/Art/Materials/M_Large_Foliage.mat"
            };
            Color[] polishedColors = new Color[]
            {
                new Color(0.32f, 0.74f, 0.28f, 1.0f),
                new Color(0.28f, 0.68f, 0.25f, 1.0f),
                new Color(0.24f, 0.64f, 0.22f, 1.0f)
            };

            for (int i = 0; i < leafMatPaths.Length; i++)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(leafMatPaths[i]);
                if (mat != null)
                {
                    mat.SetColor("_BaseColor", polishedColors[i]);
                    mat.SetFloat("_Smoothness", 0.24f);
                    mat.SetFloat("_Cull", 0); // Giữ hai mặt cho phiến lá cây
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        private static void SetupPostProcessingAndLighting(GameObject envGo)
        {
            // A. Post-Processing VolumeProfile
            string profilePath = $"{MatDir}/TropicalFantasy_VolumeProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            // Bloom
            if (!profile.TryGet(out Bloom bloom))
                bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.intensity.Override(0.65f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.70f);

            // Color Adjustments
            if (!profile.TryGet(out ColorAdjustments colorAdj))
                colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.active = true;
            colorAdj.postExposure.Override(0.12f);
            colorAdj.contrast.Override(12f);
            colorAdj.saturation.Override(20f);
            colorAdj.colorFilter.Override(new Color(1.0f, 0.98f, 0.95f));

            // Vignette
            if (!profile.TryGet(out Vignette vignette))
                vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.42f);

            EditorUtility.SetDirty(profile);

            // B. GameObject PostProcessing_Volume
            var volTrans = envGo.transform.Find("PostProcessing_Volume");
            GameObject volGo = volTrans != null ? volTrans.gameObject : new GameObject("PostProcessing_Volume");
            if (volTrans == null) volGo.transform.SetParent(envGo.transform, false);

            var volume = volGo.GetComponent<Volume>();
            if (volume == null) volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            // C. Camera UniversalAdditionalCameraData
            var cam = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.48f, 0.74f, 0.88f); // Bầu trời ngọc lam nhiệt đới
                var uacd = cam.GetComponent<UniversalAdditionalCameraData>();
                if (uacd != null)
                {
                    uacd.renderPostProcessing = true;
                }
            }

            // D. Directional Light & RenderSettings
            var sceneLight = Object.FindAnyObjectByType<Light>();
            if (sceneLight != null && sceneLight.type == LightType.Directional)
            {
                sceneLight.color = new Color(1.0f, 0.97f, 0.90f);
                sceneLight.intensity = 1.15f;
                sceneLight.shadows = LightShadows.Soft;
                sceneLight.shadowStrength = 0.72f;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.54f, 0.64f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.68f, 0.82f, 0.92f);
            RenderSettings.fogDensity = 0.0035f;
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Environment"))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Environment");
            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Environment", "Materials");
            if (!AssetDatabase.IsValidFolder(MeshDir))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Environment", "Meshes");
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Environment", "Prefabs");
        }

        private static void SaveAllMeshesAsAssets(GameObject root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            int count = 0;
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;

                string currentPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (string.IsNullOrEmpty(currentPath))
                {
                    string hierarchy = "";
                    for (var node = mf.transform; node != root.transform; node = node.parent)
                        hierarchy = node.name + "_" + node.GetSiblingIndex() + "_" + hierarchy;
                    string safeName = SanitizeFileName(hierarchy + mf.sharedMesh.name);
                    string targetPath = $"{MeshDir}/{safeName}.asset";

                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(targetPath);
                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(mf.sharedMesh, existing);
                        mf.sharedMesh = existing;
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(Object.Instantiate(mf.sharedMesh), targetPath);
                        mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(targetPath);
                    }

                    var mc = mf.GetComponent<MeshCollider>();
                    if (mc != null)
                    {
                        mc.sharedMesh = mf.sharedMesh;
                    }

                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TropicalEnvironmentSetup] Saved {count} meshes to '{MeshDir}'.");
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Replace(" ", "_");
        }

        private static Material GetOrCreateFireflyMaterial()
        {
            string texturePath = MatDir + "/Firefly_SoftDot.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "Firefly_SoftDot", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float radius = new Vector2((x + .5f) / 16 - 1, (y + .5f) / 16 - 1).magnitude;
                        float alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(1 - radius));
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                texture.Apply();
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            string materialPath = MatDir + "/M_Fireflies.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default")) { name = "M_Fireflies" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.mainTexture = texture;
            material.color = Color.white;
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness, float metallic, int cullMode = 2)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isTerrain = name == "M_Tropical_Grass" || name == "M_Warm_Dirt" || name == "M_Sandy_Shore" || name == "M_Lake_Bed";
            var targetShader = isTerrain
                ? (Shader.Find("Universal Render Pipeline/Custom/TerrainLit") ?? Shader.Find("Universal Render Pipeline/Lit"))
                : Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(targetShader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = targetShader;
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Surface", 0);
            mat.SetFloat("_Cull", cullMode);
            mat.SetOverrideTag("RenderType", "");
            mat.renderQueue = -1;
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateTransparentMaterial(string name, Color baseColor, float smoothness, float metallic, int cullMode = 2)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_Cull", cullMode);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateEmissiveMaterial(string name, Color baseColor, Color emissionColor, float smoothness, int cullMode = 2)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Cull", cullMode);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
