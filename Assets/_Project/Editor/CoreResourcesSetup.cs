using System;
using CayTheGioi.Bootstrap;
using CayTheGioi.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CayTheGioi.Editor
{
    public static class CoreResourcesSetup
    {
        [MenuItem("Cây Thế Giới/T01/Prepare HUD")]
        public static void PrepareHud()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play before HUD setup.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ProjectSetup.ScenePath) throw new InvalidOperationException("Open MainScene first.");
            // Unity's Cylinder primitive has a CapsuleCollider: its radius follows X/Z scale,
            // turning this flat ground disk into a huge invisible sphere that occludes taps.
            var ground = GameObject.Find("World/Ground");
            var collider = ground != null ? ground.GetComponent<CapsuleCollider>() : null;
            if (collider != null)
            {
                var mesh = collider.GetComponent<MeshFilter>();
                if (mesh == null || mesh.sharedMesh == null) throw new InvalidOperationException("Ground mesh missing.");
                var bounds = mesh.sharedMesh.bounds;
                var obj = collider.gameObject;
                Undo.DestroyObjectImmediate(collider);
                var box = Undo.AddComponent<BoxCollider>(obj); box.center = bounds.center; box.size = bounds.size;
            }
            var hud = UnityEngine.Object.FindAnyObjectByType<FoundationHudView>();
            var canvas = hud.GetComponent<Canvas>();
            var resources = hud.transform.Find("Resources") ?? hud.transform.Find("SafeArea/Resources");
            var title = hud.transform.Find("Title") ?? hud.transform.Find("SafeArea/Title");
            var stage = hud.transform.Find("Stage") ?? hud.transform.Find("SafeArea/Stage");
            var controls = hud.transform.Find("Controls") ?? hud.transform.Find("SafeArea/Controls");
            var safe = hud.transform.Find("SafeArea") as RectTransform;
            if (safe == null) { safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>(); safe.SetParent(hud.transform, false); }
            foreach (var t in new[] { resources, title, stage, controls }) t.SetParent(safe, false);
            controls.GetComponent<Text>().text = "Tap cây để tích Sức sống (+5%)\nChuột phải hoặc kéo một ngón để xoay camera";
            controls.GetComponent<Text>().color = new Color(0.94f, 0.97f, 0.92f);
            foreach (var t in new[] { resources, title, stage, controls }) t.GetComponent<Text>().raycastTarget = false;
            var panelRect = safe.Find("ResourceBackdrop");
            if (panelRect == null) { panelRect = new GameObject("ResourceBackdrop", typeof(RectTransform), typeof(Image)).transform; panelRect.SetParent(safe, false); }
            panelRect.SetAsFirstSibling();
            var panel = panelRect.GetComponent<Image>();
            panel.color = new Color(0.93f, 0.97f, 0.94f, 0.94f); panel.raycastTarget = true;
            var layout = hud.GetComponent<SafeAreaHudLayout>() ?? hud.gameObject.AddComponent<SafeAreaHudLayout>();
            layout.Configure(safe, (RectTransform)resources, (RectTransform)stage, (RectTransform)title, (RectTransform)controls);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<GameBootstrapper>();
            if (bootstrap.GetComponent<OnlineTickDriver>() == null) bootstrap.gameObject.AddComponent<OnlineTickDriver>();
            if (bootstrap.GetComponent<TreeInputAdapter>() == null) bootstrap.gameObject.AddComponent<TreeInputAdapter>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.SetDirty(bootstrap.Config); AssetDatabase.SaveAssets();
            Debug.Log("[CayTheGioi] T01 HUD and adapters prepared; existing world preserved.");
        }

        [MenuItem("Cây Thế Giới/T01/Debug/Root 1 Leaves 0")]
        public static void RootOnly() => Scenario(1, 0, 0, 0);
        [MenuItem("Cây Thế Giới/T01/Debug/Root 0 Leaves 1 Water 1")]
        public static void LeavesOnly() => Scenario(0, 1, 1, 0);
        [MenuItem("Cây Thế Giới/T01/Debug/Root 1 Bank 5s")]
        public static void BuffBoundary() => Scenario(1, 0, 0, 5);
        [MenuItem("Cây Thế Giới/T01/Debug/Bank 3590 Tap 49")]
        public static void BankCap() { Scenario(0, 0, 0, 3590); Bootstrap().State.vitality.tapProgress = 49; Refresh(); }
        [MenuItem("Cây Thế Giới/T01/Debug/Toggle DEAD")]
        public static void ToggleDead() { var b = Bootstrap(); b.State.run.life = b.State.run.life == "ALIVE" ? "DEAD" : "ALIVE"; b.Input.Cancel(); Refresh(); }
        public static void Scenario(int root, int leaves, double water, double bank)
        {
            var b = Bootstrap(); b.State.run.life = "ALIVE"; b.State.run.levels.root = root; b.State.run.levels.leaves = leaves;
            b.State.run.water = water; b.State.run.growth = 0; b.State.vitality.bankSeconds = bank; b.State.vitality.tapProgress = 0;
            b.Input.Cancel(); Refresh();
        }
        private static GameBootstrapper Bootstrap()
        {
            var b = UnityEngine.Object.FindAnyObjectByType<GameBootstrapper>();
            if (!EditorApplication.isPlaying || b == null || !b.IsInitialized) throw new InvalidOperationException("Enter MainScene Play first.");
            return b;
        }
        private static void Refresh()
        {
            UnityEngine.Object.FindAnyObjectByType<FoundationHudView>().Refresh();
            UnityEngine.Object.FindAnyObjectByType<UpgradePanelView>()?.Refresh();
            UnityEngine.Object.FindAnyObjectByType<EvolutionPanelView>()?.Refresh();
        }
    }
}
