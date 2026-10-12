using System.Collections.Generic;
using UnityEngine;

namespace CayTheGioi.MapEnvironment
{
    [ExecuteAlways]
    public sealed class WorldEnvironmentController : MonoBehaviour
    {
        [SerializeField] private WorldEnvironmentConfig config = new WorldEnvironmentConfig();

        [Header("Materials")]
        [SerializeField] private Material matGrass;
        [SerializeField] private Material matWarmDirt;
        [SerializeField] private Material matSandyShore;
        [SerializeField] private Material matLakeBed;
        [SerializeField] private Material matLakeWater;
        [SerializeField] private Material matWaterfall;
        [SerializeField] private Material matMossyRock;
        [SerializeField] private Material matTropicalFoliage;
        [SerializeField] private Material matCoralFlower;
        [SerializeField] private Material matMushroomStem;
        [SerializeField] private Material matMushroomCap;
        [SerializeField] private Material matPalmTrunk;
        [SerializeField] private Material matFireflies;

        [Header("References")]
        [SerializeField] private Transform terrainRoot;
        [SerializeField] private Transform decorationsRoot;
        [SerializeField] private ParticleSystem firefliesParticle;

        public WorldEnvironmentConfig Config => config;

        public void BindMaterials(
            Material grass, Material dirt, Material sand, Material lakeBed,
            Material water, Material waterfall, Material rock, Material foliage,
            Material flower, Material mushStem, Material mushCap, Material palmTrunk, Material fireflies = null)
        {
            matGrass = grass;
            matWarmDirt = dirt;
            matSandyShore = sand;
            matLakeBed = lakeBed;
            matLakeWater = water;
            matWaterfall = waterfall;
            matMossyRock = rock;
            matTropicalFoliage = foliage;
            matCoralFlower = flower;
            matMushroomStem = mushStem;
            matMushroomCap = mushCap;
            matPalmTrunk = palmTrunk;
            matFireflies = fireflies;
        }

        public void BuildEnvironment()
        {
            if (config == null || !config.IsValid)
            {
                config = new WorldEnvironmentConfig();
            }

            // Dọn dẹp con cũ nếu có
            ClearChildren();

            // 1. Tạo Terrain hình cầu lớn
            BuildTerrain();

            // 2. Tạo Hồ nước & Thác nước
            BuildWater();

            // 3. Tạo Các cụm trang trí (Đá, Cây lá lớn, Dương xỉ, Hoa, Nấm sáng, Cọ mini)
            BuildDecorations();

            // 4. Tạo Đom đóm huyền ảo
            BuildFireflies();
        }

        private void ClearChildren()
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform child in transform)
            {
                toDestroy.Add(child.gameObject);
            }
            foreach (var go in toDestroy)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }

        private void BuildTerrain()
        {
            var terrainGo = new GameObject("Planet_Terrain");
            terrainGo.transform.SetParent(transform, false);
            terrainRoot = terrainGo.transform;

            var mf = terrainGo.AddComponent<MeshFilter>();
            var mr = terrainGo.AddComponent<MeshRenderer>();
            var mc = terrainGo.AddComponent<MeshCollider>();

            var terrainMesh = SphericalWorldTerrainBuilder.BuildTerrainMesh(config);
            mf.sharedMesh = terrainMesh;
            mc.sharedMesh = terrainMesh;

            mr.sharedMaterials = new Material[]
            {
                matGrass,
                matWarmDirt,
                matSandyShore,
                matLakeBed
            };
        }

        private void BuildWater()
        {
            var waterParent = new GameObject("Water_System");
            waterParent.transform.SetParent(transform, false);

            // Mặt nước hồ ngọc lam
            var lakeGo = new GameObject("Lake_Surface");
            lakeGo.transform.SetParent(waterParent.transform, false);
            var lakeMf = lakeGo.AddComponent<MeshFilter>();
            var lakeMr = lakeGo.AddComponent<MeshRenderer>();
            lakeMf.sharedMesh = SphericalWorldTerrainBuilder.BuildLakeWaterMesh(config);
            lakeMr.sharedMaterial = matLakeWater;

            // Thác nước
            var waterfallGo = new GameObject("Waterfall");
            waterfallGo.transform.SetParent(waterParent.transform, false);
            var wfMf = waterfallGo.AddComponent<MeshFilter>();
            var wfMr = waterfallGo.AddComponent<MeshRenderer>();
            wfMf.sharedMesh = SphericalWorldTerrainBuilder.BuildWaterfallMesh(config);
            wfMr.sharedMaterial = matWaterfall;
        }

        private void BuildDecorations()
        {
            var decoGo = new GameObject("Decorations");
            decoGo.transform.SetParent(transform, false);
            decorationsRoot = decoGo.transform;

            var prng = new System.Random(config.seed);

            // A. CỤM VÁCH ĐÁ THÁC NƯỚC (Bên trái quanh thác và mép hồ)
            var cliffRockParent = new GameObject("Rocks_Waterfall_Cliff");
            cliffRockParent.transform.SetParent(decorationsRoot, false);

            Vector3[] cliffRockPositions = new Vector3[]
            {
                new Vector3(-2.6f, 0, 0.1f),
                new Vector3(-3.2f, 0, 0.25f),
                new Vector3(-3.5f, 0, -0.3f),
                new Vector3(-2.4f, 0, -0.5f),
                new Vector3(-3.6f, 0, -0.9f),
                new Vector3(-2.3f, 0, -1.2f),
                new Vector3(-1.8f, 0, -2.6f),
                new Vector3(-3.9f, 0, -2.8f)
            };
            Vector3[] cliffRockScales = new Vector3[]
            {
                new Vector3(1.1f, 0.55f, 1.0f), // Clear the large-tree roots at the minimum camera pitch.
                new Vector3(1.4f, 1.2f, 1.3f),
                new Vector3(1.2f, 0.85f, 1.1f),
                new Vector3(0.85f, 0.65f, 0.8f),
                new Vector3(0.95f, 0.7f, 0.9f),
                new Vector3(0.7f, 0.5f, 0.7f),
                new Vector3(0.6f, 0.45f, 0.6f),
                new Vector3(0.8f, 0.55f, 0.75f)
            };

            for (int i = 0; i < cliffRockPositions.Length; i++)
            {
                PlaceRock(cliffRockParent.transform, cliffRockPositions[i], cliffRockScales[i], 100 + i);
            }

            // B. CỤM ĐÁ LỚN ĐỐI DIỆN (Bên phải để cân bằng bố cục, dời ra ngoài R >= 3.8m để không che tiền cảnh camera)
            var rightRockParent = new GameObject("Rocks_Right_Formation");
            rightRockParent.transform.SetParent(decorationsRoot, false);

            Vector3[] rightRockPositions = new Vector3[]
            {
                new Vector3(3.8f, 0, -1.0f),
                new Vector3(4.2f, 0, -1.8f),
                new Vector3(3.6f, 0, -2.5f),
                new Vector3(4.5f, 0, 0.2f),
                new Vector3(3.9f, 0, 1.2f),
                new Vector3(2.5f, 0, 3.8f),   // Phía sau
                new Vector3(-1.8f, 0, 3.8f)   // Phía sau bên trái
            };
            Vector3[] rightRockScales = new Vector3[]
            {
                new Vector3(1.2f, 0.85f, 1.15f),
                new Vector3(1.05f, 0.80f, 0.95f),
                new Vector3(0.85f, 0.65f, 0.80f),
                new Vector3(1.1f, 0.75f, 1.05f),
                new Vector3(0.80f, 0.55f, 0.75f),
                new Vector3(0.95f, 0.65f, 0.85f),
                new Vector3(0.80f, 0.55f, 0.75f)
            };

            for (int i = 0; i < rightRockPositions.Length; i++)
            {
                PlaceRock(rightRockParent.transform, rightRockPositions[i], rightRockScales[i], 200 + i);
            }

            // C. DƯƠNG XỈ NHIỆT ĐỚI (Ferns)
            var fernParent = new GameObject("Foliage_Ferns");
            fernParent.transform.SetParent(decorationsRoot, false);

            Vector3[] fernPositions = new Vector3[]
            {
                new Vector3(-3.6f, 0, 0.6f), // Keep fern fronds behind the low-pitch viewing corridor.
                new Vector3(-3.5f, 0, -0.2f),
                new Vector3(-2.6f, 0, -1.5f),
                new Vector3(-2.2f, 0, -2.6f),
                new Vector3(-3.8f, 0, -2.6f),
                new Vector3(3.2f, 0, -1.5f),
                new Vector3(3.6f, 0, -2.4f),
                new Vector3(4.0f, 0, -1.8f),
                new Vector3(3.4f, 0, 1.0f),
                new Vector3(2.4f, 0, -3.6f),
                new Vector3(-0.8f, 0, -4.2f)
            };
            for (int i = 0; i < fernPositions.Length; i++)
            {
                PlaceFern(fernParent.transform, fernPositions[i], 0.75f + (float)prng.NextDouble() * 0.35f, (float)prng.NextDouble() * 360f);
            }

            // D. B03: CÂY LÁ LỚN NHIỆT ĐỚI - Dời ra rìa thế giới R >= 4.6m để không chắn tiền cảnh camera gameplay khi orbit 360°
            var broadleafParent = new GameObject("Foliage_Broadleaves");
            broadleafParent.transform.SetParent(decorationsRoot, false);

            Vector3[] broadleafPositions = new Vector3[]
            {
                new Vector3(-4.6f, 0, 0.8f),   // Sau vách đá thác nước
                new Vector3(-4.2f, 0, -2.8f),  // Rìa sau hồ nước
                new Vector3(4.8f, 0, -1.2f),   // Rìa phải ngoài xa
                new Vector3(4.6f, 0, 1.6f),    // Rìa phải phía sau
                new Vector3(0.5f, 0, 4.8f)     // Phía sau cây chính ngoài xa
            };
            for (int i = 0; i < broadleafPositions.Length; i++)
            {
                PlaceBroadleaf(broadleafParent.transform, broadleafPositions[i], 0.85f + (float)prng.NextDouble() * 0.35f, (float)prng.NextDouble() * 360f);
            }

            // E. HOA SAN HÔ NHIỆT ĐỚI (Coral flowers)
            var flowerParent = new GameObject("Foliage_CoralFlowers");
            flowerParent.transform.SetParent(decorationsRoot, false);

            Vector3[] flowerPositions = new Vector3[]
            {
                new Vector3(-2.1f, 0, -1.8f),
                new Vector3(-1.8f, 0, -3.4f),
                new Vector3(2.3f, 0, -1.6f),
                new Vector3(2.8f, 0, -2.8f),
                new Vector3(0.6f, 0, -3.8f)
            };
            for (int i = 0; i < flowerPositions.Length; i++)
            {
                PlaceCoralFlower(flowerParent.transform, flowerPositions[i], (float)prng.NextDouble() * 360f);
            }

            // F. NẤM PHÁT SÁNG DẠ QUANG (Bioluminescent Cyan Mushrooms)
            var mushroomParent = new GameObject("Mushrooms_Bioluminescent");
            mushroomParent.transform.SetParent(decorationsRoot, false);

            // Cụm 1: Gần vách đá thác nước
            PlaceMushroomCluster(mushroomParent.transform, new Vector3(-2.3f, 0, -0.8f), 4, 301);
            // Cụm 2: Ở kẽ đá cụm bên phải
            PlaceMushroomCluster(mushroomParent.transform, new Vector3(2.7f, 0, -1.1f), 3, 302);
            // Cụm 3: Ở bờ đá phía ngoài hồ
            PlaceMushroomCluster(mushroomParent.transform, new Vector3(-3.7f, 0, -2.6f), 3, 303);

            // G. CÂY CỌ MINI Ở VIỀN CHÂN TRỜI (Mini Horizon Palms)
            var palmParent = new GameObject("Foliage_MiniPalms");
            palmParent.transform.SetParent(decorationsRoot, false);

            Vector3[] palmPositions = new Vector3[]
            {
                new Vector3(-7.5f, 0, -4.5f),
                new Vector3(-8.8f, 0, 1.5f),
                new Vector3(-5.5f, 0, 7.2f),
                new Vector3(3.2f, 0, 8.5f),
                new Vector3(7.8f, 0, 4.2f),
                new Vector3(8.5f, 0, -2.8f),
                new Vector3(5.8f, 0, -6.5f),
                new Vector3(-1.8f, 0, -8.6f)
            };
            for (int i = 0; i < palmPositions.Length; i++)
                PlaceMiniPalm(palmParent.transform, palmPositions[i], 1.5f + (float)prng.NextDouble() * .4f, (float)prng.NextDouble() * 360);
            // H. KHÓM CỎ THẤP NHIỆT ĐỚI (Low Grass Tufts bám sát nền đất cao <= 0.15m)
            var grassTuftParent = new GameObject("Foliage_GrassTufts");
            grassTuftParent.transform.SetParent(decorationsRoot, false);

            Vector3[] grassTuftPositions = new Vector3[]
            {
                new Vector3(-1.4f, 0, -0.6f),
                new Vector3(-1.1f, 0, -1.5f),
                new Vector3(-0.9f, 0, 1.2f),
                new Vector3(1.2f, 0, 0.9f),
                new Vector3(1.5f, 0, -1.0f),
                new Vector3(0.9f, 0, -1.8f),
                new Vector3(-0.4f, 0, -2.6f),
                new Vector3(1.6f, 0, -3.2f),
                new Vector3(-1.8f, 0, 2.2f),
                new Vector3(2.2f, 0, 1.8f),
                new Vector3(-2.8f, 0, -3.2f),
                new Vector3(2.8f, 0, -3.4f)
            };
            for (int i = 0; i < grassTuftPositions.Length; i++)
            {
                PlaceGrassTuft(grassTuftParent.transform, grassTuftPositions[i], 0.12f + (float)prng.NextDouble() * 0.05f, (float)prng.NextDouble() * 360f);
            }

            // I. SỎI NHỎ MỊN (Smooth Pebbles rải rác gần lối mòn và chân đá cao <= 0.06m)
            var pebbleParent = new GameObject("Rocks_Pebbles");
            pebbleParent.transform.SetParent(decorationsRoot, false);

            Vector3[] pebblePositions = new Vector3[]
            {
                new Vector3(-0.5f, 0, -0.8f),
                new Vector3(0.6f, 0, -1.2f),
                new Vector3(0.3f, 0, -2.1f),
                new Vector3(1.1f, 0, -2.8f),
                new Vector3(-1.6f, 0, -2.0f),
                new Vector3(-2.0f, 0, -0.4f),
                new Vector3(2.4f, 0, -0.8f),
                new Vector3(2.2f, 0, -2.2f),
                new Vector3(0.8f, 0, -3.8f),
                new Vector3(-0.2f, 0, -3.5f)
            };
            for (int i = 0; i < pebblePositions.Length; i++)
            {
                PlacePebble(pebbleParent.transform, pebblePositions[i], 0.06f + (float)prng.NextDouble() * 0.04f, (float)prng.NextDouble() * 360f);
            }
        }

        private void PlaceGrassTuft(Transform parent, Vector3 localPos, float height, float rotY)
        {
            var go = new GameObject("GrassTuft");
            go.transform.SetParent(parent, false);

            float y = SphericalWorldTerrainBuilder.CalculateHeight(localPos.x, localPos.z, config);
            Vector3 normal = CalculateSphereNormal(localPos.x, y, localPos.z);

            go.transform.position = new Vector3(localPos.x, y, localPos.z) - normal * 0.02f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, rotY, 0);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TropicalPropsBuilder.BuildGrassTuftMesh(height);
            mr.sharedMaterial = matGrass;
        }

        private void PlacePebble(Transform parent, Vector3 localPos, float radius, float rotY)
        {
            var go = new GameObject("Pebble");
            go.transform.SetParent(parent, false);

            float y = SphericalWorldTerrainBuilder.CalculateHeight(localPos.x, localPos.z, config);
            Vector3 normal = CalculateSphereNormal(localPos.x, y, localPos.z);

            go.transform.position = new Vector3(localPos.x, y, localPos.z) - normal * 0.015f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, rotY, 0);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TropicalPropsBuilder.BuildPebbleMesh(radius);
            mr.sharedMaterial = matMossyRock;
        }

        private void PlaceRock(Transform parent, Vector3 localPos, Vector3 scale, int seed)
        {
            var go = new GameObject($"Rock_{seed}");
            go.transform.SetParent(parent, false);

            float y = SphericalWorldTerrainBuilder.CalculateHeight(localPos.x, localPos.z, config);
            Vector3 worldPos = new Vector3(localPos.x, y, localPos.z);
            Vector3 normal = CalculateSphereNormal(localPos.x, y, localPos.z);

            go.transform.position = worldPos - normal * (scale.y * 0.18f);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, seed * 37f, 0);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TropicalPropsBuilder.BuildRockMesh(seed, scale);
            mr.sharedMaterial = matMossyRock;

            // Thêm MeshCollider convex để chặn raycast tap xuyên đá vào cây
            var col = go.AddComponent<MeshCollider>();
            col.convex = true;
            col.sharedMesh = mf.sharedMesh;
        }

        private void PlaceFern(Transform parent, Vector3 localPos, float scale, float rotY)
        {
            var go = new GameObject("Fern");
            go.transform.SetParent(parent, false);

            float y = SphericalWorldTerrainBuilder.CalculateHeight(localPos.x, localPos.z, config);
            Vector3 normal = CalculateSphereNormal(localPos.x, y, localPos.z);

            go.transform.position = new Vector3(localPos.x, y, localPos.z) - normal * 0.04f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, rotY, 0);
            go.transform.localScale = Vector3.one * scale;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TropicalPropsBuilder.BuildFernMesh();
            mr.sharedMaterial = matTropicalFoliage;
        }

        private void PlaceBroadleaf(Transform parent, Vector3 localPos, float scale, float rotY)
        {
            var go = new GameObject("Broadleaf");
            go.transform.SetParent(parent, false);

            float y = SphericalWorldTerrainBuilder.CalculateHeight(localPos.x, localPos.z, config);
            Vector3 normal = CalculateSphereNormal(localPos.x, y, localPos.z);

            go.transform.position = new Vector3(localPos.x, y, localPos.z) - normal * 0.05f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, rotY, 0);
            go.transform.localScale = Vector3.one * scale;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TropicalPropsBuilder.BuildBroadleafMesh();
            mr.sharedMaterial = matTropicalFoliage;
        }

        private void PlaceCoralFlower(Transform parent, Vector3 localPos, float rotY)
        {
            var go = new GameObject("CoralFlower");
            go.transform.SetParent(parent, false);

            float y = SphericalWorldTerrainBuilder.CalculateHeight(localPos.x, localPos.z, config);
            Vector3 normal = CalculateSphereNormal(localPos.x, y, localPos.z);

            go.transform.position = new Vector3(localPos.x, y, localPos.z) - normal * 0.03f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, rotY, 0);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TropicalPropsBuilder.BuildCoralFlowerMesh();
            mr.sharedMaterial = matCoralFlower;
        }

        private void PlaceMushroomCluster(Transform parent, Vector3 localPos, int count, int seed)
        {
            var clusterGo = new GameObject($"MushroomCluster_{seed}");
            clusterGo.transform.SetParent(parent, false);

            float clusterY = SphericalWorldTerrainBuilder.CalculateHeight(localPos.x, localPos.z, config);
            Vector3 clusterNormal = CalculateSphereNormal(localPos.x, clusterY, localPos.z);

            clusterGo.transform.position = new Vector3(localPos.x, clusterY, localPos.z) - clusterNormal * 0.04f;
            clusterGo.transform.rotation = Quaternion.FromToRotation(Vector3.up, clusterNormal);

            var prng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var shroomGo = new GameObject($"Shroom_{seed}_{i}");
                shroomGo.transform.SetParent(clusterGo.transform, false);

                float offsetDist = (i == 0) ? 0f : (0.12f + (float)prng.NextDouble() * 0.16f);
                float offsetAngle = (float)prng.NextDouble() * Mathf.PI * 2f;
                shroomGo.transform.localPosition = new Vector3(Mathf.Cos(offsetAngle) * offsetDist, 0, Mathf.Sin(offsetAngle) * offsetDist);
                shroomGo.transform.localRotation = Quaternion.Euler(((float)prng.NextDouble() - 0.5f) * 18f, (float)prng.NextDouble() * 360f, ((float)prng.NextDouble() - 0.5f) * 18f);

                float capRadius = 0.14f + (float)prng.NextDouble() * 0.12f;
                float stemHeight = 0.22f + (float)prng.NextDouble() * 0.18f;

                var mf = shroomGo.AddComponent<MeshFilter>();
                var mr = shroomGo.AddComponent<MeshRenderer>();
                var shroomMesh = TropicalPropsBuilder.BuildMushroomMesh(capRadius, stemHeight);
                shroomMesh.name = $"Glow_Mushroom_Mesh_{seed}_{i}";
                mf.sharedMesh = shroomMesh;
                mr.sharedMaterials = new Material[] { matMushroomStem, matMushroomCap };
            }

            // Điểm sáng ấm dịu nhẹ xanh ngọc dạ quang (Cyan point light nhẹ, không shadow)
            var lightGo = new GameObject("Shroom_GlowLight");
            lightGo.transform.SetParent(clusterGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.35f, 0);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.12f, 0.90f, 0.92f); // Cyan bioluminescent
            light.intensity = 0.65f;
            light.range = 2.2f;
            light.shadows = LightShadows.None;
        }

        private void PlaceMiniPalm(Transform parent, Vector3 localPos, float height, float rotY)
        {
            var go = new GameObject("MiniPalm");
            go.transform.SetParent(parent, false);

            float y = SphericalWorldTerrainBuilder.CalculateHeight(localPos.x, localPos.z, config);
            Vector3 normal = CalculateSphereNormal(localPos.x, y, localPos.z);

            go.transform.position = new Vector3(localPos.x, y, localPos.z) - normal * 0.22f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, rotY, 0);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TropicalPropsBuilder.BuildMiniPalmMesh(height);
            mr.sharedMaterials = new Material[] { matPalmTrunk, matTropicalFoliage };
        }

        private void BuildFireflies()
        {
            var ffGo = new GameObject("VFX_Fireflies");
            ffGo.transform.SetParent(transform, false);
            ffGo.transform.localPosition = new Vector3(0, 1.2f, -1.0f);

            var ps = ffGo.AddComponent<ParticleSystem>();
            firefliesParticle = ps;

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 6.0f;
            main.startSpeed = 0.22f;
            main.startSize = 0.09f;
            main.startColor = new Color(1.0f, 0.88f, 0.35f, 0.85f); // Vàng ấm dịu dàng
            main.maxParticles = 35;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 5.0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 4.5f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.18f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.3f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0.9f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var renderer = ffGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = matFireflies;
        }

        private Vector3 CalculateSphereNormal(float x, float y, float z)
        {
            Vector3 sphereCenter = new Vector3(0, -config.sphereRadius, 0);
            return (new Vector3(x, y, z) - sphereCenter).normalized;
        }
    }
}
