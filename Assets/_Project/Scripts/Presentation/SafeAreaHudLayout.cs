using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    public sealed class SafeAreaHudLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform resources;
        [SerializeField] private RectTransform stage;
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform controls;
        [SerializeField] private RectTransform backdrop;
        private Vector2 lastSize;
        private Rect lastSafe;
        public void Configure(RectTransform safe, RectTransform resourceRect, RectTransform stageRect, RectTransform titleRect, RectTransform controlsRect)
        { safeArea = safe; resources = resourceRect; stage = stageRect; title = titleRect; controls = controlsRect; backdrop = safe.Find("ResourceBackdrop") as RectTransform; Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height)); }
        private void Update()
        {
            var size = new Vector2(Screen.width, Screen.height);
            if (size != lastSize || lastSafe != Screen.safeArea) Apply(Screen.safeArea, size);
        }
        public void Apply(Rect safe, Vector2 size)
        {
            if (safeArea == null || size.x <= 0 || size.y <= 0) return;
            lastSize = size; lastSafe = safe;
            safeArea.anchorMin = safe.position / size;
            safeArea.anchorMax = (safe.position + safe.size) / size;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            bool portrait = safe.width < safe.height;
            // Expand the short dimension in Canvas units to keep text readable in either orientation.
            var scaler = GetComponent<CanvasScaler>();
            scaler.referenceResolution = portrait ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = portrait ? 0 : 1;
            controls.GetComponent<Text>().fontSize = portrait ? 21 : 17;
            Place(title, new Vector2(0, 1), new Vector2(28, -18), new Vector2(620, 42));
            title.GetComponent<Text>().fontSize = 28;
            Place(stage, new Vector2(0, 1), new Vector2(28, -62), new Vector2(420, 32));
            stage.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            stage.GetComponent<Text>().fontSize = 20;
            var upgradeView = GetComponent<UpgradePanelView>();
            Place(controls, new Vector2(0.5f, 0), new Vector2(0, portrait ? 120 : 118), new Vector2(portrait ? 980 : 1200, 48));
            var hud = GetComponent<FoundationHudView>();
            if (hud != null) hud.LayoutCards();
            if (upgradeView != null) upgradeView.Layout(portrait);
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
        { rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = offset; rect.sizeDelta = size; }
    }
}
