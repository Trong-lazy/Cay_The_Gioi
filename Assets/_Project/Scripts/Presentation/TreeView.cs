using CayTheGioi.Configuration;
using UnityEngine;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class TreeView : MonoBehaviour
    {
        [SerializeField] private Transform modelSlot;
        [SerializeField] private Transform rootAnchor;
        [SerializeField] private Transform leavesAnchor;
        [SerializeField] private Transform trunkAnchor;
        [SerializeField] private Transform lightningTargetAnchor;
        [SerializeField] private GameObject currentModel;
        [SerializeField] private string currentStageId;
        private TreeStageAnchors currentStageAnchors;
        private GameplayInputConfig tapSettings = new GameplayInputConfig();
        public Transform ModelSlot => modelSlot;
        public Transform RootAnchor => rootAnchor;
        public Transform LeavesAnchor => leavesAnchor;
        public Transform TrunkAnchor => trunkAnchor;
        public Transform LightningTargetAnchor => lightningTargetAnchor != null ? lightningTargetAnchor : leavesAnchor;
        public TreeStageAnchors CurrentStageAnchors => currentStageAnchors != null ? currentStageAnchors : (currentModel != null ? currentModel.GetComponent<TreeStageAnchors>() : null);
        public string CurrentStageId => currentStageId;

        public void ConfigureTapArea(GameplayInputConfig settings)
        {
            tapSettings = settings ?? new GameplayInputConfig();
            ApplyStageAnchorsAndCollider();
        }

        public void ShowStage(StageVisualDefinition visual)
        {
            if (visual == null || visual.prefab == null || modelSlot == null)
                throw new System.InvalidOperationException("TreeView stage visual or model slot is missing.");
            if (currentModel == null || currentStageId != visual.stageId)
            {
                var nextModel = Instantiate(visual.prefab, modelSlot);
                if (currentModel != null)
                {
                    currentModel.SetActive(false);
                    if (Application.isPlaying) Destroy(currentModel);
                    else DestroyImmediate(currentModel);
                }
                currentModel = nextModel;
                currentModel.name = "StageModel";
                currentModel.transform.localPosition = Vector3.zero;
                currentModel.transform.localRotation = Quaternion.identity;
                currentModel.transform.localScale = Vector3.one;
                currentStageId = visual.stageId;
            }
            ApplyStageAnchorsAndCollider();
            var anim = GetComponent<TreeAnimationController>();
            if (anim != null) anim.OnStageChanged(currentModel, currentStageId);
        }

        private void LateUpdate()
        {
            if (currentStageAnchors != null)
            {
                SyncAnchor(rootAnchor, currentStageAnchors.RootAnchor);
                SyncAnchor(trunkAnchor, currentStageAnchors.TrunkAuraAnchor);
                SyncAnchor(leavesAnchor, currentStageAnchors.LeavesCenterAnchor);
                SyncAnchor(lightningTargetAnchor, currentStageAnchors.LightningTargetAnchor != null ? currentStageAnchors.LightningTargetAnchor : currentStageAnchors.LeavesCenterAnchor);
            }
        }

        private void ApplyStageAnchorsAndCollider()
        {
            if (currentModel == null) return;
            EnsureLightningAnchor();
            currentStageAnchors = currentModel.GetComponent<TreeStageAnchors>();
            var collider = GetComponent<SphereCollider>();
            if (collider == null) collider = gameObject.AddComponent<SphereCollider>();

            // A stable gameplay volume includes gaps between leaves and survives flutter.
            // Keep the model's colliders intact for physical occlusion and other queries.
            collider.enabled = true;
            collider.isTrigger = true;
            if (currentStageId == "seedling")
            {
                collider.center = tapSettings.seedlingTapCenter;
                collider.radius = tapSettings.seedlingTapRadius;
            }
            else if (currentStageId == "young_tree")
            {
                collider.center = tapSettings.youngTreeTapCenter;
                collider.radius = tapSettings.youngTreeTapRadius;
            }
            else
            {
                var renderers = currentModel.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    collider.center = transform.InverseTransformPoint(bounds.center);
                    float scale = Mathf.Max(.0001f, Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Max(Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z))));
                    collider.radius = (Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) + tapSettings.automaticTapPadding) / scale;
                }
                else if (currentStageAnchors != null)
                {
                    collider.center = currentStageAnchors.TapColliderCenter;
                    collider.radius = currentStageAnchors.TapColliderRadius;
                }
            }

            if (currentStageAnchors != null)
            {
                SyncAnchor(rootAnchor, currentStageAnchors.RootAnchor);
                SyncAnchor(trunkAnchor, currentStageAnchors.TrunkAuraAnchor);
                SyncAnchor(leavesAnchor, currentStageAnchors.LeavesCenterAnchor);
                SyncAnchor(lightningTargetAnchor, currentStageAnchors.LightningTargetAnchor != null ? currentStageAnchors.LightningTargetAnchor : currentStageAnchors.LeavesCenterAnchor);
                return;
            }

            var rList = currentModel.GetComponentsInChildren<Renderer>();
            if (rList.Length > 0)
            {
                var bounds = rList[0].bounds;
                foreach (var renderer in rList) bounds.Encapsulate(renderer.bounds);
                if (rootAnchor != null) { rootAnchor.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z); rootAnchor.localScale = Vector3.one; }
                if (trunkAnchor != null) { trunkAnchor.position = bounds.center; trunkAnchor.localScale = Vector3.one; }
                if (leavesAnchor != null) { leavesAnchor.position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z); leavesAnchor.localScale = Vector3.one; }
                if (lightningTargetAnchor != null && leavesAnchor != null) { lightningTargetAnchor.position = leavesAnchor.position; lightningTargetAnchor.localScale = Vector3.one; }
            }
        }

        private void EnsureLightningAnchor()
        {
            if (lightningTargetAnchor != null) return;
            var existing = transform.Find("LightningTargetAnchor");
            if (existing != null) { lightningTargetAnchor = existing; return; }
            var go = new GameObject("LightningTargetAnchor");
            go.transform.SetParent(transform, false);
            lightningTargetAnchor = go.transform;
        }

        private static void SyncAnchor(Transform target, Transform source)
        {
            if (target == null || source == null) return;
            target.position = source.position;
            target.rotation = source.rotation;
            target.localScale = source.localScale;
        }

#if UNITY_EDITOR
        public void Configure(Transform slot, Transform root, Transform leaves, Transform trunk, GameObject model, string stageId, Transform lightning = null)
        {
            modelSlot = slot; rootAnchor = root; leavesAnchor = leaves; trunkAnchor = trunk; lightningTargetAnchor = lightning; currentModel = model; currentStageId = stageId;
            ApplyStageAnchorsAndCollider();
        }
#endif
    }
}
