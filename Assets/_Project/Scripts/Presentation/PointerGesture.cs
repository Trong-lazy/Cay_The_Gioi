using UnityEngine;

namespace CayTheGioi.Presentation
{
    // One gesture owns the command path. A second finger cancels that gesture.
    public sealed class PointerGesture
    {
        private int owner;
        private Vector2 origin;
        private double started;
        private float threshold;
        private bool active, dragged;
        public bool Dragged => dragged;
        public void Begin(int pointer, Vector2 position, double now, float dragThreshold, bool blocked)
        { Cancel(); if (blocked) return; owner = pointer; origin = position; started = now; threshold = dragThreshold; active = true; }
        public void Move(int pointer, Vector2 position)
        { if (active && pointer == owner && (position - origin).sqrMagnitude > threshold * threshold) dragged = true; }
        public bool Release(int pointer, Vector2 position, double now, double maxTapSeconds, bool blocked)
        {
            Move(pointer, position);
            bool tap = active && pointer == owner && !dragged && !blocked && now - started >= 0 && now - started <= maxTapSeconds;
            Cancel(); return tap;
        }
        public void Cancel() { active = false; dragged = false; }
    }
}
