using System;
using UnityEngine;

namespace CayTheGioi.MapEnvironment
{
    [Serializable]
    public sealed class WorldEnvironmentConfig
    {
        [Header("Sphere Planet Terrain")]
        [Tooltip("Bán kính hình cầu của thế giới nhỏ (mét).")]
        public float sphereRadius = 16.0f;

        [Tooltip("Bán kính giới hạn hiển thị của mặt đất (mét).")]
        public float terrainOuterRadius = 12.5f;

        [Tooltip("Độ phân giải lưới địa hình.")]
        public int gridResolution = 48;

        [Header("Lake & Waterfall")]
        [Tooltip("Tọa độ tâm hồ nước (X, Z).")]
        public Vector2 lakeCenter = new Vector2(-3.3f, -1.9f);

        [Tooltip("Bán kính lòng hồ nước.")]
        public float lakeRadius = 1.85f;

        [Tooltip("Độ sâu tối đa của lòng hồ.")]
        public float lakeDepth = 0.38f;

        [Tooltip("Độ cao mực nước hồ so với gốc (mét).")]
        public float waterLevelY = -0.48f;

        [Tooltip("Tọa độ đỉnh vách đá thác nước (X, Z).")]
        public Vector2 waterfallOrigin = new Vector2(-2.9f, -0.2f);

        [Header("Dirt Path")]
        [Tooltip("Điểm xuất phát lối mòn gần gốc cây.")]
        public Vector2 pathStart = new Vector2(0.1f, -0.7f);

        [Tooltip("Điểm giữa uốn lượn lối mòn.")]
        public Vector2 pathMid = new Vector2(0.8f, -2.4f);

        [Tooltip("Điểm cuối lối mòn.")]
        public Vector2 pathEnd = new Vector2(1.5f, -4.5f);

        [Tooltip("Bề rộng lối mòn.")]
        public float pathWidth = 0.95f;

        [Header("Environment Props")]
        [Tooltip("Hạt giống cố định để sinh bố cục props tất định.")]
        public int seed = 42;

        public bool IsValid =>
            sphereRadius > 5.0f &&
            terrainOuterRadius > 2.0f &&
            terrainOuterRadius < sphereRadius &&
            gridResolution >= 16 &&
            lakeRadius > 0.5f &&
            lakeDepth > 0.05f;
    }
}
