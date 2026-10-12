using System.Collections.Generic;
using System.Globalization;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    public sealed class OfflineRewardView : MonoBehaviour
    {
        private GameObject blocker;
        private RectTransform panel;
        private Text message;
        private readonly Queue<OfflineReward> queue = new Queue<OfflineReward>();
        public Button CloseButton { get; private set; }
        public bool IsVisible => blocker != null && blocker.activeSelf;
        public string Content => message == null ? "" : message.text;
        public void Bind()
        {
            if (blocker != null) return;
            Transform safe = transform.Find("SafeArea") ?? transform;
            blocker = new GameObject("OfflineRewardBlocker", typeof(RectTransform), typeof(Image)); blocker.transform.SetParent(safe, false);
            var rect = (RectTransform)blocker.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);
            panel = new GameObject("OfflineRewardPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>(); panel.SetParent(rect, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f); panel.sizeDelta = new Vector2(860, 460);
            panel.GetComponent<Image>().color = new Color(0.91f, 0.96f, 0.9f);
            message = new GameObject("OfflineRewardText", typeof(RectTransform), typeof(Text)).GetComponent<Text>(); message.transform.SetParent(panel, false);
            var textRect = (RectTransform)message.transform; textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0, 1);
            textRect.anchoredPosition = new Vector2(28, -24); textRect.sizeDelta = new Vector2(804, 306);
            message.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); message.fontSize = 30; message.color = new Color(0.08f, 0.23f, 0.15f); message.raycastTarget = false;
            CloseButton = new GameObject("OfflineContinue", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>(); CloseButton.transform.SetParent(panel, false);
            var buttonRect = (RectTransform)CloseButton.transform; buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(0, 28); buttonRect.sizeDelta = new Vector2(340, 88); CloseButton.GetComponent<Image>().color = new Color(0.74f, 0.87f, 0.75f);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>(); label.transform.SetParent(buttonRect, false);
            var labelRect = (RectTransform)label.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label.font = message.font; label.fontSize = 30; label.color = message.color; label.alignment = TextAnchor.MiddleCenter; label.text = "Tiếp tục"; label.raycastTarget = false;
            CloseButton.onClick.AddListener(Dismiss); blocker.SetActive(false);
        }
        public void Show(OfflineReward reward)
        {
            if (reward == null || reward.ConsideredSeconds <= 0) return;
            Bind(); queue.Enqueue(reward);
            if (!IsVisible) DisplayNext();
        }
        public void Clear() { queue.Clear(); if(blocker!=null)blocker.SetActive(false); }
        public void Dismiss() { blocker.SetActive(false); DisplayNext(); }
        private void DisplayNext()
        {
            if (queue.Count == 0) return;
            var reward = queue.Dequeue();
            message.text = string.Format(CultureInfo.InvariantCulture,
                "Sản xuất khi vắng mặt\nĐã xét {0:0} giây • tối đa {4:0.##} giờ • tốc độ {5:0.##}%\nWater {1:+0.##;-0.##;0}    Growth {2:+0.##;-0.##;0}\n{3}\nSức sống và thời gian Weather/status được giữ.",
                reward.ConsideredSeconds, reward.WaterDelta, reward.GrowthDelta,
                reward.Dead ? "Cây đã chết: không sản xuất offline." : reward.Capped ? "Thời gian vượt giới hạn không nhận thêm." : "Tiến độ và checkpoint đã được lưu.", reward.MaxSeconds / 3600, reward.RateFactor * 100);
            message.text+="\nQuang hợp "+UpgradePanelView.Format(reward.PhotosynthesisDelta)+" · Linh khí "+UpgradePanelView.Format(reward.SpiritDelta);
            blocker.SetActive(true);
            // Load/save errors have priority over this informational popup.
            var saveBlocker = blocker.transform.parent.Find("SaveLoadBlocker");
            if (saveBlocker != null) saveBlocker.SetAsLastSibling();
        }
        private void LateUpdate()
        {
            if (panel == null) return;
            panel.sizeDelta = new Vector2(Mathf.Min(860, ((RectTransform)blocker.transform).rect.width - 40), 460);
            ((RectTransform)message.transform).sizeDelta = new Vector2(panel.rect.width - 56, 306);
        }
    }
}
