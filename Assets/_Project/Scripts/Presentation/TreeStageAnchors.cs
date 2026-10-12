using UnityEngine;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class TreeStageAnchors : MonoBehaviour
    {
        [SerializeField] private Transform lightningTargetAnchor;
        [SerializeField] private Transform leavesCenterAnchor;
        [SerializeField] private Transform trunkAuraAnchor;
        [SerializeField] private Transform rootAnchor;
        [SerializeField] private Vector3 tapColliderCenter = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private float tapColliderRadius = 0.5f;
        [SerializeField] private Collider[] hitColliders;

        public Transform LightningTargetAnchor => lightningTargetAnchor;
        public Transform LeavesCenterAnchor => leavesCenterAnchor;
        public Transform TrunkAuraAnchor => trunkAuraAnchor;
        public Transform RootAnchor => rootAnchor;
        public Vector3 TapColliderCenter => tapColliderCenter;
        public float TapColliderRadius => tapColliderRadius;
        public Collider[] HitColliders => hitColliders;

        public bool HasActiveHitColliders()
        {
            if (hitColliders != null)
            {
                for (int i = 0; i < hitColliders.Length; i++)
                {
                    if (hitColliders[i] != null && hitColliders[i].enabled && hitColliders[i].gameObject.activeInHierarchy)
                        return true;
                }
            }

            var childColliders = GetComponentsInChildren<Collider>(false);
            for (int i = 0; i < childColliders.Length; i++)
            {
                if (childColliders[i] != null && childColliders[i].enabled)
                    return true;
            }
            return false;
        }

        public void Configure(
            Transform lightningTarget,
            Transform leavesCenter,
            Transform trunkAura,
            Transform root,
            Vector3 colliderCenter,
            float colliderRadius,
            Collider[] stageHitColliders = null)
        {
            lightningTargetAnchor = lightningTarget;
            leavesCenterAnchor = leavesCenter;
            trunkAuraAnchor = trunkAura;
            rootAnchor = root;
            tapColliderCenter = colliderCenter;
            tapColliderRadius = colliderRadius;
            hitColliders = stageHitColliders;
        }
    }
}
