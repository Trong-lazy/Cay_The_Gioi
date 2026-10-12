using System;
using UnityEngine;

namespace CayTheGioi.Presentation
{
    [Serializable]
    public sealed class GameplayInputConfig
    {
        public float dragMillimetres = 2;
        public float fallbackDragScreenFraction = 0.012f;
        public float maximumTapSeconds = 0.4f;
        public float minimumDragPixels = 4;
        public float maximumDragScreenFraction = 0.04f;
        [Header("Tap volume (world units on the default Tree scale)")]
        public Vector3 seedlingTapCenter = new Vector3(0, .16f, 0);
        public float seedlingTapRadius = .25f;
        public Vector3 youngTreeTapCenter = new Vector3(0, .65f, 0);
        public float youngTreeTapRadius = .58f;
        public float automaticTapPadding = .08f;
        public float DragPixels(float dpi, float width, float height)
        {
            float shortSide = Mathf.Min(width, height);
            float raw = dpi > 0 && !float.IsNaN(dpi) && !float.IsInfinity(dpi) ? dpi * dragMillimetres / 25.4f : shortSide * fallbackDragScreenFraction;
            return Mathf.Clamp(raw, minimumDragPixels, Mathf.Max(minimumDragPixels, shortSide * maximumDragScreenFraction));
        }
        public bool IsValid => dragMillimetres > 0 && fallbackDragScreenFraction > 0 && maximumTapSeconds > 0 && minimumDragPixels > 0 && maximumDragScreenFraction > 0 &&
            float.IsFinite(dragMillimetres) && float.IsFinite(fallbackDragScreenFraction) && float.IsFinite(maximumTapSeconds) && float.IsFinite(minimumDragPixels) && float.IsFinite(maximumDragScreenFraction) &&
            seedlingTapRadius > 0 && youngTreeTapRadius > 0 && automaticTapPadding >= 0 &&
            float.IsFinite(seedlingTapRadius) && float.IsFinite(youngTreeTapRadius) && float.IsFinite(automaticTapPadding) &&
            Finite(seedlingTapCenter) && Finite(youngTreeTapCenter);
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
