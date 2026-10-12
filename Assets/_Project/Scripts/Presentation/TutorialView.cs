using System;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class TutorialView : MonoBehaviour
    {
        private TutorialSystem tutorial;
        private GameState state;
        private TreeInputAdapter input;
        private ToastView toastView;

        private GameObject bannerRoot;
        private Text titleText;
        private Text descText;
        private Button confirmButton;
        private Text confirmButtonText;

        private string activeStepId;
        private bool modalWasOpen;
        public string ActiveStepId => activeStepId;
        public bool IsVisible => bannerRoot != null && bannerRoot.activeSelf;

        public void Bind(TutorialSystem tutorialSystem, GameState gameState, TreeInputAdapter adapter, ToastView toast)
        {
            tutorial = tutorialSystem ?? throw new ArgumentNullException(nameof(tutorialSystem));
            state = gameState ?? throw new ArgumentNullException(nameof(gameState));
            input = adapter;
            toastView = toast;

            tutorial.StateChanged += Refresh;
            tutorial.StepCompleted += OnStepCompleted;

            Transform safe = transform.Find("SafeArea") ?? transform;

            // Banner Container
            bannerRoot = new GameObject("TutorialBanner", typeof(RectTransform), typeof(Image));
            bannerRoot.transform.SetParent(safe, false);

            var rect = (RectTransform)bannerRoot.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(24, 150);
            rect.sizeDelta = new Vector2(620, 116);

            var img = bannerRoot.GetComponent<Image>();
            img.color = new Color(0.1f, 0.16f, 0.22f, 0.92f);
            img.raycastTarget = false;

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(bannerRoot.transform, false);
            var titleRect = (RectTransform)titleGo.transform;
            titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(0, 1);
            titleRect.anchoredPosition = new Vector2(20, -12);
            titleRect.sizeDelta = new Vector2(580, 28);

            titleText = titleGo.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.35f, 0.85f, 0.55f);
            titleText.raycastTarget = false;

            // Description
            var descGo = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            descGo.transform.SetParent(bannerRoot.transform, false);
            var descRect = (RectTransform)descGo.transform;
            descRect.anchorMin = descRect.anchorMax = descRect.pivot = new Vector2(0, 1);
            descRect.anchoredPosition = new Vector2(20, -42);
            descRect.sizeDelta = new Vector2(426, 62);

            descText = descGo.GetComponent<Text>();
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 16;
            descText.color = new Color(0.92f, 0.95f, 0.98f);
            descText.raycastTarget = false;

            // Confirm Button (chỉ dùng cho bước dialogue hoặc xác nhận)
            var btnGo = new GameObject("ConfirmBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(bannerRoot.transform, false);
            var btnRect = (RectTransform)btnGo.transform;
            btnRect.anchorMin = btnRect.anchorMax = btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-20, 0);
            btnRect.sizeDelta = new Vector2(130, 48);

            btnGo.GetComponent<Image>().color = new Color(0.24f, 0.6f, 0.4f);
            confirmButton = btnGo.GetComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);

            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = (RectTransform)btnTextGo.transform;
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = btnTextRect.offsetMax = Vector2.zero;

            confirmButtonText = btnTextGo.GetComponent<Text>();
            confirmButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            confirmButtonText.fontSize = 16;
            confirmButtonText.fontStyle = FontStyle.Bold;
            confirmButtonText.alignment = TextAnchor.MiddleCenter;
            confirmButtonText.color = Color.white;
            confirmButtonText.text = "Đã hiểu";

            Refresh();
        }

        private void OnStepCompleted(string stepId)
        {
            if (toastView != null)
            {
                var def = tutorial != null ? tutorial.GetStepDefinition(stepId) : null;
                string name = def != null ? def.Title : stepId;
                toastView.Show($"Hoàn thành hướng dẫn: {name}!");
            }
            Refresh();
        }

        private void OnConfirmClicked()
        {
            if (tutorial != null && !string.IsNullOrEmpty(activeStepId))
            {
                tutorial.ConfirmDialogueStep(activeStepId);
            }
        }

        public void Refresh()
        {
            if (bannerRoot == null || tutorial == null || state == null) return;
            if (ModalOpen()) { bannerRoot.SetActive(false); return; }

            // DEAD modal có độ ưu tiên cao nhất, ẩn tutorial để tránh che lấp hoặc softlock
            if (state.run.life == "DEAD")
            {
                bannerRoot.SetActive(false);
                return;
            }

            activeStepId = tutorial.GetCurrentStepId();
            if (string.IsNullOrEmpty(activeStepId))
            {
                bannerRoot.SetActive(false);
                return;
            }

            var def = tutorial.GetStepDefinition(activeStepId);
            if (def == null)
            {
                bannerRoot.SetActive(false);
                return;
            }

            bannerRoot.SetActive(true);
            if (titleText != null) titleText.text = $"HƯỚNG DẪN: {def.Title}";
            if (descText != null) descText.text = def.Description;

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(def.RequiresConfirmation);
            }
        }

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        private void LateUpdate()
        {
            if (bannerRoot == null) return;
            bool modalOpen=ModalOpen();
            if(modalOpen!=modalWasOpen) { modalWasOpen=modalOpen; Refresh(); }
            if (!bannerRoot.activeSelf) return;

            // Nếu cây chết trong quá trình chơi, lập tức ẩn banner
            if (state != null && state.run.life == "DEAD")
            {
                bannerRoot.SetActive(false);
                return;
            }

            if (GetComponent<ReferenceUiLayout>() != null || GetComponentInParent<ReferenceUiLayout>() != null) return;
            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) return;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            var rect = (RectTransform)bannerRoot.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(24, 150);

            float maxWidth = ((RectTransform)bannerRoot.transform.parent).rect.width - 40;
            rect.sizeDelta = new Vector2(Mathf.Min(620, maxWidth), 116);
        }

        private void OnDestroy()
        {
            if (tutorial != null)
            {
                tutorial.StateChanged -= Refresh;
                tutorial.StepCompleted -= OnStepCompleted;
            }
        }

        private bool ModalOpen()
        {
            var safe=transform.Find("SafeArea") ?? transform;
            foreach(var name in new[]{"SettingsBlocker","ShopBlocker","AchievementBlocker","RebirthBlocker","OfflineRewardBlocker","SaveLoadBlocker"})
            {
                var modal=safe.Find(name);
                if(modal!=null && modal.gameObject.activeSelf) return true;
            }
            return false;
        }
    }
}
