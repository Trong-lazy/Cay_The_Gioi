using System;
using System.Diagnostics;
using CayTheGioi.Domain;
using Unity.Profiling;
using UnityEngine;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class TreeAnimationController : MonoBehaviour
    {
        private static readonly ProfilerMarker s_TotalAnimationMarker = new ProfilerMarker("TreeAnimation.TotalUpdate");
        private static readonly ProfilerMarker s_VertexFlutterMarker = new ProfilerMarker("TreeAnimation.VertexFlutter");
        private static readonly ProfilerMarker s_ColliderRecookMarker = new ProfilerMarker("TreeAnimation.ColliderRecook");

        public struct FramePerfMetrics
        {
            public double vertexCalculationMs;
            public double colliderRecookMs;
            public double totalMs;
        }

        public FramePerfMetrics LastFramePerf { get; private set; }
        public bool EnableColliderRecookPerFrame { get; set; } = false;

        private TreeView treeView;
        private WeatherSystem weather;
        private HealthStatusSystem health;

        private Transform currentModelTransform;
        private string currentStageId;
        private Transform trunkPart;
        private Transform foliagePart;
        private Transform rootsPart;

        private MeshFilter foliageMeshFilter;
        private MeshCollider foliageMeshCollider;
        private Mesh animatedFoliageMesh;
        private Vector3[] baseFoliageVertices;
        private Vector3[] animatedFoliageVertices;
        private float[] foliageWeights;
        private float[] precomputedSinPhi;
        private float[] precomputedCosPhi;
        private int[] dynamicVertexIndices;
        private float foliageScaleNorm = 1.0f;

        private Transform leavesAnchorTransform;
        private Transform lightningAnchorTransform;
        private Transform trunkAuraAnchorTransform;

        private Vector3 initialLeavesAnchorLocalPos;
        private Vector3 initialLightningAnchorLocalPos;
        private Vector3 initialTrunkAuraLocalPos;

        private Quaternion initialTrunkLocalRot = Quaternion.identity;
        private Quaternion initialFoliageLocalRot = Quaternion.identity;
        private Quaternion initialRootsLocalRot = Quaternion.identity;

        private Quaternion currentTilt = Quaternion.identity;
        private float shudderTimer;
        private float shudderIntensity;

        private CayTheGioi.Configuration.TreeAnimationConfig animConfig = new CayTheGioi.Configuration.TreeAnimationConfig();

        public void Bind(TreeView view, WeatherSystem weatherSystem, HealthStatusSystem healthSystem, CayTheGioi.Configuration.TreeAnimationConfig config = null)
        {
            Unsubscribe();
            treeView = view;
            weather = weatherSystem;
            health = healthSystem;
            if (config != null) animConfig = config;

            if (weather != null)
            {
                weather.LightningStruck += OnLightningStruck;
            }

            if (treeView != null && treeView.CurrentStageAnchors != null)
            {
                OnStageChanged(treeView.CurrentStageAnchors.gameObject, treeView.CurrentStageId);
            }
        }

        public void OnStageChanged(GameObject stageModel, string stageId)
        {
            CleanupFoliageMesh();

            currentModelTransform = stageModel != null ? stageModel.transform : null;
            currentStageId = stageId;
            currentTilt = Quaternion.identity;
            shudderTimer = 0f;

            if (currentModelTransform == null) return;

            // Locate visual parts
            trunkPart = FindChild(currentModelTransform, "Trunk_Branches");
            foliagePart = FindChild(currentModelTransform, "Foliage");
            rootsPart = FindChild(currentModelTransform, "Roots");

            if (trunkPart != null) initialTrunkLocalRot = trunkPart.localRotation;
            if (foliagePart != null) initialFoliageLocalRot = foliagePart.localRotation;
            if (rootsPart != null) initialRootsLocalRot = rootsPart.localRotation;

            // Locate anchors
            leavesAnchorTransform = FindChild(currentModelTransform, "Anchor_LeavesCenter");
            lightningAnchorTransform = FindChild(currentModelTransform, "Anchor_LightningTarget");
            trunkAuraAnchorTransform = FindChild(currentModelTransform, "Anchor_TrunkAura");

            if (leavesAnchorTransform != null) initialLeavesAnchorLocalPos = leavesAnchorTransform.localPosition;
            if (lightningAnchorTransform != null) initialLightningAnchorLocalPos = lightningAnchorTransform.localPosition;
            if (trunkAuraAnchorTransform != null) initialTrunkAuraLocalPos = trunkAuraAnchorTransform.localPosition;

            // Setup animated foliage mesh with scale normalization and branch attachment weights
            if (foliagePart != null)
            {
                Vector3 lossy = foliagePart.lossyScale;
                foliageScaleNorm = Mathf.Max(0.0001f, (lossy.x + lossy.y + lossy.z) / 3f);

                foliageMeshFilter = foliagePart.GetComponent<MeshFilter>();
                foliageMeshCollider = foliagePart.GetComponent<MeshCollider>();
                if (foliageMeshFilter != null && foliageMeshFilter.sharedMesh != null)
                {
                    var sourceMesh = foliageMeshFilter.sharedMesh;
                    baseFoliageVertices = sourceMesh.vertices;
                    animatedFoliageVertices = new Vector3[baseFoliageVertices.Length];
                    Array.Copy(baseFoliageVertices, animatedFoliageVertices, baseFoliageVertices.Length);

                    PrecomputeBranchAttachmentWeights();

                    animatedFoliageMesh = Instantiate(sourceMesh);
                    animatedFoliageMesh.name = sourceMesh.name + "_AnimatedInstance";
                    animatedFoliageMesh.MarkDynamic();
                    var expBounds = sourceMesh.bounds;
                    expBounds.Expand(0.3f);
                    animatedFoliageMesh.bounds = expBounds;
                    foliageMeshFilter.mesh = animatedFoliageMesh;
                    if (foliageMeshCollider != null) foliageMeshCollider.sharedMesh = animatedFoliageMesh;
                }
            }
        }

        private void PrecomputeBranchAttachmentWeights()
        {
            if (baseFoliageVertices == null || baseFoliageVertices.Length == 0) return;

            foliageWeights = new float[baseFoliageVertices.Length];

            // Subsample trunk/branches vertices to find closest branch attachment point for each foliage vertex
            Mesh trunkMesh = null;
            if (trunkPart != null)
            {
                var mf = trunkPart.GetComponent<MeshFilter>();
                if (mf != null) trunkMesh = mf.sharedMesh;
            }

            if (trunkMesh != null && trunkMesh.vertexCount > 0)
            {
                var trunkVerts = trunkMesh.vertices;
                int stride = Math.Max(1, trunkVerts.Length / 120);
                int sampleCount = (trunkVerts.Length + stride - 1) / stride;
                var trunkSamples = new Vector3[sampleCount];
                int sIdx = 0;
                for (int i = 0; i < trunkVerts.Length; i += stride)
                {
                    if (sIdx < sampleCount)
                    {
                        trunkSamples[sIdx++] = trunkPart.TransformPoint(trunkVerts[i]);
                    }
                }

                float minD = float.MaxValue;
                float maxD = float.MinValue;
                var dists = new float[baseFoliageVertices.Length];

                for (int i = 0; i < baseFoliageVertices.Length; i++)
                {
                    Vector3 fv = foliagePart.TransformPoint(baseFoliageVertices[i]);
                    float closestSq = float.MaxValue;
                    for (int j = 0; j < sIdx; j++)
                    {
                        float dSq = (fv - trunkSamples[j]).sqrMagnitude;
                        if (dSq < closestSq) closestSq = dSq;
                    }
                    float dist = Mathf.Sqrt(closestSq);
                    dists[i] = dist;
                    if (dist < minD) minD = dist;
                    if (dist > maxD) maxD = dist;
                }

                // Strictly pin leaf stems/petioles attached to branches (w = 0)
                float thresholdFrac = currentStageId == "large_tree" ? 0.55f : 0.08f;
                float attachThreshold = minD + (maxD - minD) * thresholdFrac;
                float range = Mathf.Max(0.001f, (maxD - attachThreshold) * 0.45f);
                for (int i = 0; i < baseFoliageVertices.Length; i++)
                {
                    if (dists[i] <= attachThreshold)
                    {
                        foliageWeights[i] = 0f;
                    }
                    else
                    {
                        float t = Mathf.Clamp01((dists[i] - attachThreshold) / range);
                        foliageWeights[i] = t * t * (3f - 2f * t);
                    }
                }
            }
            else
            {
                // Fallback: weight increases with height
                for (int i = 0; i < baseFoliageVertices.Length; i++)
                {
                    foliageWeights[i] = 1.0f;
                }
            }

            precomputedSinPhi = new float[baseFoliageVertices.Length];
            precomputedCosPhi = new float[baseFoliageVertices.Length];
            var dynamicList = new System.Collections.Generic.List<int>(baseFoliageVertices.Length);
            for (int i = 0; i < baseFoliageVertices.Length; i++)
            {
                Vector3 v0 = baseFoliageVertices[i];
                float phi = v0.x * 2.4f + v0.y * 3.2f + v0.z * 1.8f;
                precomputedSinPhi[i] = Mathf.Sin(phi);
                precomputedCosPhi[i] = Mathf.Cos(phi);
                if (foliageWeights[i] > 0.0001f)
                {
                    dynamicList.Add(i);
                }
            }
            dynamicVertexIndices = dynamicList.ToArray();
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent == null) return null;
            var direct = parent.Find(name);
            if (direct != null) return direct;
            var modelGroup = parent.Find("Model/" + name);
            if (modelGroup != null) return modelGroup;
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        public void TriggerShudder(float intensity = 1.0f)
        {
            shudderTimer = 0.35f;
            shudderIntensity = intensity;
        }

        private void OnLightningStruck(string regionId)
        {
            TriggerShudder(1.2f);
        }

        private void Update()
        {
            using (s_TotalAnimationMarker.Auto())
            {
                var swTotal = Stopwatch.StartNew();
                double vertexTimeMs = 0.0;
                double colliderRecookTimeMs = 0.0;

                if (currentModelTransform == null)
                {
                    if (treeView != null && treeView.CurrentStageAnchors != null)
                    {
                        OnStageChanged(treeView.CurrentStageAnchors.gameObject, treeView.CurrentStageId);
                    }
                    if (currentModelTransform == null) return;
                }

                string weatherId = weather != null ? weather.CurrentWeatherId : "calm";
                bool isThunderstorm = weatherId == "thunderstorm";
                float time = Time.time;

                // ── 1. Calculate Trunk & Canopy Wind Bending ──────────────────────
                Quaternion targetTilt = Quaternion.identity;

                if (isThunderstorm)
                {
                    // Stage tilt amplitude factor (small sprout has subtle sway, large tree has majestic bend)
                    float maxAngle = currentStageId == "seedling" ? animConfig.seedlingMaxTiltAngle : (currentStageId == "young_tree" ? animConfig.youngTreeMaxTiltAngle : animConfig.largeTreeMaxTiltAngle);

                    // Wind gust rhythm using Perlin noise
                    float gustNoise = Mathf.PerlinNoise(time * 0.95f, 0.42f);
                    float gustCycle = Mathf.Lerp(0.35f, 1.0f, gustNoise);
                    float windAngleZ = -Mathf.Sin(time * 1.5f) * (maxAngle * 0.25f) - (maxAngle * 0.75f * gustCycle);
                    float windAngleX = Mathf.Cos(time * 1.1f) * (maxAngle * 0.20f);

                    targetTilt = Quaternion.Euler(windAngleX, 0f, windAngleZ);
                }

                // Smooth transition between weather states
                currentTilt = Quaternion.Slerp(currentTilt, targetTilt, Time.deltaTime * animConfig.tiltSmoothSpeed);

                // Lightning strike impact shudder
                Quaternion finalTilt = currentTilt;
                if (shudderTimer > 0f)
                {
                    shudderTimer -= Time.deltaTime;
                    float shudderFreq = 42f;
                    float shudderDamp = Mathf.Exp(-(0.35f - shudderTimer) * 11f);
                    float shudderPitch = Mathf.Sin(time * shudderFreq) * 2.8f * shudderIntensity * shudderDamp;
                    float shudderRoll = Mathf.Cos(time * shudderFreq * 1.15f) * 2.8f * shudderIntensity * shudderDamp;
                    finalTilt *= Quaternion.Euler(shudderPitch, 0f, shudderRoll);
                }

                // Apply tilt to Trunk and Foliage together around base (0,0,0)
                // Roots remain strictly pinned at ground level
                if (trunkPart != null) trunkPart.localRotation = finalTilt * initialTrunkLocalRot;
                if (foliagePart != null) foliagePart.localRotation = finalTilt * initialFoliageLocalRot;
                if (rootsPart != null) rootsPart.localRotation = initialRootsLocalRot;

                if (finalTilt != Quaternion.identity)
                {
                    Physics.SyncTransforms();
                }

                // ── 2. Sync Stage Anchors with Animated Tilt ─────────────────────
                if (leavesAnchorTransform != null) leavesAnchorTransform.localPosition = finalTilt * initialLeavesAnchorLocalPos;
                if (lightningAnchorTransform != null) lightningAnchorTransform.localPosition = finalTilt * initialLightningAnchorLocalPos;
                if (trunkAuraAnchorTransform != null) trunkAuraAnchorTransform.localPosition = finalTilt * initialTrunkAuraLocalPos;

                // ── 3. Organic Foliage Vertex Flutter with Scale Normalization ───
                if (animatedFoliageMesh != null && baseFoliageVertices != null)
                {
                    var swVertex = Stopwatch.StartNew();
                    using (s_VertexFlutterMarker.Auto())
                    {
                        float flutterSpeed = isThunderstorm ? animConfig.thunderstormFlutterSpeed : animConfig.calmFlutterSpeed;
                        float baseWorldAmp = currentStageId == "seedling" ? animConfig.seedlingFlutterAmpWorld : (currentStageId == "young_tree" ? animConfig.youngTreeFlutterAmpWorld : animConfig.largeTreeFlutterAmpWorld);
                        float worldFlutterAmp = isThunderstorm ? (baseWorldAmp * animConfig.thunderstormAmpMultiplier) : baseWorldAmp;
                        Vector3 lossy = foliagePart != null ? foliagePart.lossyScale : Vector3.one;
                        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(lossy.x));
                        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(lossy.y));
                        float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(lossy.z));

                        float sinA = Mathf.Sin(time * flutterSpeed);
                        float cosA = Mathf.Cos(time * flutterSpeed);
                        float sinB = Mathf.Sin(time * flutterSpeed * 0.85f);
                        float cosB = Mathf.Cos(time * flutterSpeed * 0.85f);
                        float sinC = Mathf.Sin(time * flutterSpeed * 1.25f);
                        float cosC = Mathf.Cos(time * flutterSpeed * 1.25f);

                        float coeffX = worldFlutterAmp / scaleX;
                        float coeffY = (worldFlutterAmp * 0.35f) / scaleY;
                        float coeffZ = worldFlutterAmp / scaleZ;

                        if (dynamicVertexIndices != null && precomputedSinPhi != null)
                        {
                            for (int k = 0; k < dynamicVertexIndices.Length; k++)
                            {
                                int i = dynamicVertexIndices[k];
                                Vector3 v0 = baseFoliageVertices[i];
                                float w = foliageWeights[i];
                                float ampX = coeffX * w;
                                float ampY = coeffY * w;
                                float ampZ = coeffZ * w;
                                float sPhi = precomputedSinPhi[i];
                                float cPhi = precomputedCosPhi[i];

                                float termA = sinA * cPhi + cosA * sPhi;
                                float termB = cosB * cPhi - sinB * sPhi;
                                float termC = sinC * cPhi + cosC * sPhi;

                                animatedFoliageVertices[i] = new Vector3(
                                    v0.x + termA * ampX,
                                    v0.y + termC * ampY,
                                    v0.z + termB * ampZ
                                );
                            }
                        }

                        animatedFoliageMesh.vertices = animatedFoliageVertices;
                    }
                    swVertex.Stop();
                    vertexTimeMs = swVertex.Elapsed.TotalMilliseconds;

                    // Transform synchronization alone does not update cooked triangle geometry.
                    if (EnableColliderRecookPerFrame && foliageMeshCollider != null)
                    {
                        var swCollider = Stopwatch.StartNew();
                        using (s_ColliderRecookMarker.Auto())
                        {
                            foliageMeshCollider.sharedMesh = null;
                            foliageMeshCollider.sharedMesh = animatedFoliageMesh;
                        }
                        swCollider.Stop();
                        colliderRecookTimeMs = swCollider.Elapsed.TotalMilliseconds;
                    }
                }

                swTotal.Stop();
                LastFramePerf = new FramePerfMetrics
                {
                    vertexCalculationMs = vertexTimeMs,
                    colliderRecookMs = colliderRecookTimeMs,
                    totalMs = swTotal.Elapsed.TotalMilliseconds
                };
            }
        }

        private void CleanupFoliageMesh()
        {
            if (animatedFoliageMesh != null)
            {
                if (Application.isPlaying) Destroy(animatedFoliageMesh);
                else DestroyImmediate(animatedFoliageMesh);
                animatedFoliageMesh = null;
            }
        }

        private void Unsubscribe()
        {
            if (weather != null)
            {
                weather.LightningStruck -= OnLightningStruck;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            CleanupFoliageMesh();
        }
    }
}
