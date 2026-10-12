using System;
using CayTheGioi.Bootstrap;
using CayTheGioi.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Editor
{
    public static class UpgradeSetup
    {
        [MenuItem("Cây Thế Giới/T02/Prepare Upgrade UI")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ProjectSetup.ScenePath) throw new InvalidOperationException("Open MainScene first.");
            var hud = UnityEngine.Object.FindAnyObjectByType<FoundationHudView>();
            var safe = hud.transform.Find("SafeArea");
            if (safe == null) throw new InvalidOperationException("T01 safe area is required.");
            if (hud.GetComponent<UpgradePanelView>() != null)
            { Debug.Log("[CayTheGioi] T02 UI exists; preserved."); return; }
            var open = MakeButton("UpgradeOpen", safe, "Upgrade", new Vector2(1, 0), new Vector2(-28, 28), new Vector2(230, 88));
            var panel = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(safe, false); panel.GetComponent<Image>().color = new Color(0.94f, 0.97f, 0.94f, 0.98f);
            Label("Heading", panel, "NÂNG CẤP", new Vector2(0, 1), new Vector2(24, -20), new Vector2(450, 50), 32);
            var close = MakeButton("Close", panel, "Đóng", new Vector2(1, 1), new Vector2(-20, -16), new Vector2(150, 60));
            var rows = new UpgradeRowView[3];
            var ids = new[] { "root", "leaves", "trunk" };
            for (int i = 0; i < ids.Length; i++)
            {
                float y = -90 - i * 150;
                var row = new GameObject(ids[i], typeof(RectTransform)).GetComponent<RectTransform>(); row.SetParent(panel, false);
                row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1); row.pivot = new Vector2(0.5f, 1); row.anchoredPosition = new Vector2(0, y); row.sizeDelta = new Vector2(-48, 144);
                rows[i] = new UpgradeRowView {
                    branchId = ids[i],
                    details = Label("Details", row, "", new Vector2(0, 1), Vector2.zero, new Vector2(610, 110), 25),
                    reason = Label("Reason", row, "", new Vector2(0, 1), new Vector2(0, -112), new Vector2(780, 32), 23),
                    button = MakeButton("Buy", row, "+1", new Vector2(1, 1), new Vector2(0, -8), new Vector2(150, 88))
                };
            }
            var feedback = Label("Feedback", panel, "", new Vector2(0, 1), new Vector2(24, -565), new Vector2(800, 50), 26);
            var view = hud.gameObject.AddComponent<UpgradePanelView>(); view.Configure(open, close, panel, feedback, rows);
            view.Layout(Screen.safeArea.width < Screen.safeArea.height); panel.gameObject.SetActive(false);
            var layout = hud.GetComponent<SafeAreaHudLayout>(); layout.Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            var config = UnityEngine.Object.FindAnyObjectByType<GameBootstrapper>().Config;
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            Debug.Log("[CayTheGioi] T02 Upgrade UI saved; existing world/config preserved.");
        }
        private static Text Label(string name, Transform parent, string content, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>(); text.transform.SetParent(parent, false);
            var rect = (RectTransform)text.transform; rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.color = new Color(0.08f, 0.23f, 0.15f); text.raycastTarget = false; text.text = content;
            return text;
        }
        private static Button MakeButton(string name, Transform parent, string caption, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>(); button.transform.SetParent(parent, false);
            var rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = offset; rect.sizeDelta = size;
            button.GetComponent<Image>().color = new Color(0.74f, 0.87f, 0.75f);
            var colors = button.colors; colors.disabledColor = new Color(0.64f, 0.64f, 0.64f, 0.6f); button.colors = colors;
            var label = Label("Label", button.transform, caption, new Vector2(0.5f, 0.5f), Vector2.zero, size, 30); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        [MenuItem("Cây Thế Giới/T02/Debug/Young Tree State (no Evolution)")]
        public static void YoungTree()
        {
            var b = Bootstrap(); b.State.run.stageId = "young_tree"; b.State.run.life = "ALIVE"; b.State.run.photosynthesis=1000; b.State.run.growth=1000; b.State.run.water = 1000; b.Input.Cancel(); Refresh();
        }
        [MenuItem("Cây Thế Giới/T02/Debug/Seedling Reset")]
        public static void SeedlingReset()
        {
            var b = Bootstrap(); b.State.run.stageId = "seedling"; b.State.run.levels.trunk = 0;
            CoreResourcesSetup.Scenario(0, 0, b.Config.Rules.initialWater, 0); Refresh();
        }
        public static void Refresh()
        {
            var b = Bootstrap();
            UnityEngine.Object.FindAnyObjectByType<UpgradePanelView>().Refresh();
            UnityEngine.Object.FindAnyObjectByType<EvolutionPanelView>()?.Refresh();
            UnityEngine.Object.FindAnyObjectByType<FoundationHudView>().Bind(b.State, b.Config.Rules.FindStage(b.State.run.stageId).displayName, b.Config.Rules.tapsPerReward);
        }
        private static GameBootstrapper Bootstrap()
        {
            var b = UnityEngine.Object.FindAnyObjectByType<GameBootstrapper>();
            if (!EditorApplication.isPlaying || b == null || !b.IsInitialized) throw new InvalidOperationException("Enter MainScene Play first.");
            return b;
        }
    }
}
