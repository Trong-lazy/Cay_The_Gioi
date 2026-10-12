using System;
using CayTheGioi.Bootstrap;
using CayTheGioi.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Editor
{
    public static class EvolutionSetup
    {
        [MenuItem("Cây Thế Giới/T03/Prepare Evolution UI")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ProjectSetup.ScenePath) throw new InvalidOperationException("Open MainScene first.");
            var upgrade = UnityEngine.Object.FindAnyObjectByType<UpgradePanelView>();
            if (upgrade.GetComponent<EvolutionPanelView>() == null)
            {
                var panel = upgrade.Panel;
                var details = Label("EvolutionDetails", panel, new Vector2(24, -610), new Vector2(610, 110), 25);
                var reason = Label("EvolutionReason", panel, new Vector2(24, -724), new Vector2(810, 40), 23);
                var evolve = Button("Evolve", panel, "Evolution", new Vector2(1, 1), new Vector2(-24, -620), new Vector2(195, 88));
                var orbit = Button("OrbitLock", panel, "Orbit: Tự do", new Vector2(0, 1), new Vector2(24, -790), new Vector2(360, 88));
                upgrade.gameObject.AddComponent<EvolutionPanelView>().Configure(details, reason, evolve, orbit);
            }
            var config = UnityEngine.Object.FindAnyObjectByType<GameBootstrapper>().Config;
            config.FindVisual("seedling").cameraTargetHeight = 0.12f;
            config.FindVisual("young_tree").cameraTargetHeight = 0.42f;
            config.FindVisual("large_tree").cameraTargetHeight = 1.18f;
            upgrade.Layout(Screen.width < Screen.height);
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[CayTheGioi] T03 UI/config saved; existing scene preserved.");
        }
        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int font)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();text.transform.SetParent(parent,false);
            var rect=(RectTransform)text.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=font;text.color=new Color(0.08f,0.23f,0.15f);text.raycastTarget=false;return text;
        }
        private static Button Button(string name, Transform parent, string caption, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var button=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)).GetComponent<Button>();button.transform.SetParent(parent,false);
            var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;
            button.GetComponent<Image>().color=new Color(0.74f,0.87f,0.75f);
            var label=Label("Label",button.transform,Vector2.zero,size,28);var labelRect=(RectTransform)label.transform;
            labelRect.anchorMin=labelRect.anchorMax=labelRect.pivot=new Vector2(0.5f,0.5f);label.text=caption;label.alignment=TextAnchor.MiddleCenter;return button;
        }
        [MenuItem("Cây Thế Giới/T03/Debug/Fund Next Evolution")]
        public static void Fund()
        {
            var b=UnityEngine.Object.FindAnyObjectByType<GameBootstrapper>();
            if (!EditorApplication.isPlaying || b==null || !b.IsInitialized) throw new InvalidOperationException("Enter MainScene Play first.");
            var q=b.Evolution.GetQuote();b.State.run.photosynthesis=q.PhotosynthesisCost+100;b.State.run.water=q.WaterCost+100;b.State.run.growth=q.GrowthCost+50;
            UnityEngine.Object.FindAnyObjectByType<FoundationHudView>().Refresh();
            UnityEngine.Object.FindAnyObjectByType<UpgradePanelView>().Refresh();
            UnityEngine.Object.FindAnyObjectByType<EvolutionPanelView>().Refresh();
        }
    }
}
