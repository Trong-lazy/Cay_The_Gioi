using UnityEngine;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class OrbitCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private float distance = 6;
        [SerializeField] private float pitch = 25;
        [SerializeField] private float yaw;
        [SerializeField] private float degreesPerPixel = 0.18f;
        public bool OrbitLocked { get; set; }
        public bool TransitionLocked { get; set; }
        public float Distance => distance;
        public float Pitch => pitch;
        public float Yaw => yaw;
        public float TargetHeight => pivot != null ? pivot.position.y : 0;
        public Camera SceneCamera => sceneCamera;

        public void SetYaw(float newYaw)
        { yaw = newYaw; ApplyPose(); }

        public void SetView(float newDistance, float newPitch)
        { distance = newDistance; pitch = newPitch; ApplyPose(); }
        public void SetStagePose(float newDistance, float newPitch, float targetHeight)
        { if (pivot != null) pivot.position = new Vector3(pivot.position.x, targetHeight, pivot.position.z); SetView(newDistance, newPitch); }

        public void Orbit(Vector2 delta)
        {
            if (OrbitLocked || TransitionLocked) return;
            yaw += delta.x * degreesPerPixel;
            pitch = Mathf.Clamp(pitch - delta.y * degreesPerPixel, 10, 70);
            ApplyPose();
        }

        private void ApplyPose()
        {
            if (pivot == null || sceneCamera == null) return;
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            sceneCamera.transform.SetPositionAndRotation(pivot.position + rotation * new Vector3(0, 0, -distance), rotation);
        }

#if UNITY_EDITOR
        public void Configure(Transform targetPivot, Camera targetCamera)
        { pivot = targetPivot; sceneCamera = targetCamera; ApplyPose(); }
#endif
    }
}
