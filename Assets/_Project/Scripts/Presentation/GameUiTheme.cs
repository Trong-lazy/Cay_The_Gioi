using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    // Presentation only: shared sprites and typography, with no gameplay writes.
    public static class GameUiTheme
    {
        public static readonly Color Ink = new Color(.16f, .25f, .16f);
        public static readonly Color Muted = new Color(.35f, .40f, .31f);
        public static Sprite Sprite(string id) => Resources.Load<Sprite>("UI/ReferencePack/" + id);

        public static void Paper(Image image, string sprite = "card_settings_camera_bg")
        {
            if (image == null) return;
            image.sprite = Sprite(sprite);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        public static void StyleButton(Button button)
        {
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = Sprite(button.name.Contains("Rebirth") ? "button_rebirth_bg" : "button_continue_bg");
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, .86f);
            colors.pressedColor = new Color(.77f, .87f, .68f);
            colors.disabledColor = new Color(.83f, .86f, .79f);
            colors.fadeDuration = .1f;
            button.colors = colors;
            foreach (var label in button.GetComponentsInChildren<Text>(true))
            {
                label.color = Ink;
                label.fontStyle = FontStyle.Bold;
                label.fontSize = Mathf.Min(label.fontSize, 22);
                label.raycastTarget = false;
                var r = (RectTransform)label.transform;
                r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one;
                r.offsetMin = new Vector2(12, 6); r.offsetMax = new Vector2(-12, -6);
            }
        }

        public static void Apply(FoundationHudView hud)
        {
            foreach (var button in hud.GetComponentsInChildren<Button>(true)) StyleButton(button);
            foreach (var image in hud.GetComponentsInChildren<Image>(true))
            {
                if (image.name.EndsWith("Panel") || image.name == "UpgradePanel") Paper(image);
                if (image.name.EndsWith("Row") || image.name.StartsWith("ShopRow_") || image.name.StartsWith("Row_")) Paper(image, "row_upgrade_bg");
                if (image.name == "TutorialBanner") Paper(image);
            }
            foreach (var text in hud.GetComponentsInChildren<Text>(true))
            {
                if (text.GetComponentInParent<Button>() != null) continue;
                if (text.GetComponentInParent<ToastView>() != null && text.name == "ToastText") continue;
                text.color = text.name=="Controls" ? new Color(.98f,.97f,.85f) : Ink;
                if(text.name=="Controls")
                {
                    var shadow=text.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
                    shadow.effectColor=new Color(0,0,0,.65f); shadow.effectDistance=new Vector2(1,-1);
                }
            }
            hud.LayoutCards();
            // One owner for reference-based responsive composition; domain/view APIs stay unchanged.
            var composition = hud.GetComponent<ReferenceUiLayout>() ?? hud.gameObject.AddComponent<ReferenceUiLayout>();
            composition.Initialize();
        }

        public static RectTransform Card(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Paper(rect.GetComponent<Image>());
            rect.GetComponent<Image>().raycastTarget = true;
            return rect;
        }

        public static Text Label(string name, Transform parent, Vector2 offset, Vector2 size, int fontSize)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            Place((RectTransform)label.transform, new Vector2(0, 1), offset, size);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Ink;
            label.raycastTarget = false;
            return label;
        }

        public static void Icon(string id, Transform parent, Vector2 offset, Vector2 size)
        {
            var image = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            Place((RectTransform)image.transform, new Vector2(0, 1), offset, size);
            image.sprite = Sprite(id);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }
    }
}
