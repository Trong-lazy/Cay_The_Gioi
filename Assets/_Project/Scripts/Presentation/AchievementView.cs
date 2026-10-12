using System;
using System.Collections.Generic;
using System.Globalization;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    public sealed class AchievementView : MonoBehaviour
    {
        private AchievementSystem achievements;
        private GameState state;
        private TreeInputAdapter input;
        private OrbitCameraRig rig;

        private GameObject blocker;
        private RectTransform panel;
        private Text titleText;
        private Text feedbackText;
        private Button closeButton;
        private readonly List<AchievementRowUI> rows = new List<AchievementRowUI>();

        private bool previousInput;
        private bool previousLock;
        private string feedback = "";

        public Button OpenButton { get; private set; }
        public bool IsVisible => blocker != null && blocker.activeSelf;

        private sealed class AchievementRowUI
        {
            public string AchievementId;
            public GameObject Root;
            public Text InfoText;
            public Button ClaimButton;
            public Text ClaimButtonText;
            public Image ButtonImage;
        }

        public void Bind(AchievementSystem achievementSystem, GameState gameState, TreeInputAdapter adapter, OrbitCameraRig cameraRig)
        {
            achievements = achievementSystem ?? throw new ArgumentNullException(nameof(achievementSystem));
            state = gameState ?? throw new ArgumentNullException(nameof(gameState));
            input = adapter;
            rig = cameraRig;

            Transform safe = transform.Find("SafeArea") ?? transform;

            // 1. Nút mở bảng thành tựu
            OpenButton = CreateButton("AchievementOpen", safe, "Thành tựu", new Vector2(1, 1), new Vector2(-28, -24), new Vector2(240, 60));
            OpenButton.onClick.AddListener(Open);

            // 2. Blocker nền mờ
            blocker = new GameObject("AchievementBlocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(safe, false);
            var blockerRect = (RectTransform)blocker.transform;
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            // 3. Panel trung tâm
            panel = new GameObject("AchievementPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(blockerRect, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(980, 840);
            panel.GetComponent<Image>().color = new Color(0.93f, 0.97f, 0.94f);

            // 4. Tiêu đề
            titleText = CreateLabel("Title", panel, new Vector2(0, 1), new Vector2(28, -20), new Vector2(924, 70), 30, FontStyle.Bold);

            // 5. Container các row
            var listContainer = new GameObject("ListContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            listContainer.SetParent(panel, false);
            listContainer.anchorMin = new Vector2(0, 1);
            listContainer.anchorMax = new Vector2(1, 1);
            listContainer.pivot = new Vector2(0.5f, 1);
            listContainer.anchoredPosition = new Vector2(0, -95);
            listContainer.sizeDelta = new Vector2(-56, 620);

            // Tạo các item row cho 6 achievement
            var all = achievements.GetAllStatuses();
            float yPos = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var def = all[i];
                var rowObj = new GameObject("Row_" + def.AchievementId, typeof(RectTransform), typeof(Image));
                rowObj.transform.SetParent(listContainer, false);
                var rowRect = (RectTransform)rowObj.transform;
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(1, 1);
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.anchoredPosition = new Vector2(0, yPos);
                rowRect.sizeDelta = new Vector2(0, 90);
                rowObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

                var info = CreateLabel("Info", rowRect, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(680, 80), 22);
                info.alignment = TextAnchor.MiddleLeft;

                string achId = def.AchievementId;
                var claimBtn = CreateButton("ClaimBtn", rowRect, "Nhận 10 Coin", new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(200, 60));
                claimBtn.onClick.AddListener(() => OnClaimClicked(achId));

                rows.Add(new AchievementRowUI
                {
                    AchievementId = achId,
                    Root = rowObj,
                    InfoText = info,
                    ClaimButton = claimBtn,
                    ClaimButtonText = claimBtn.GetComponentInChildren<Text>(),
                    ButtonImage = claimBtn.GetComponent<Image>()
                });

                yPos -= 98;
            }

            // 6. Feedback & Close button
            feedbackText = CreateLabel("Feedback", panel, new Vector2(0, 0), new Vector2(28, 28), new Vector2(620, 60), 22);
            feedbackText.alignment = TextAnchor.MiddleLeft;

            closeButton = CreateButton("CloseBtn", panel, "Đóng", new Vector2(1, 0), new Vector2(-28, 28), new Vector2(240, 64));
            closeButton.onClick.AddListener(Close);

            blocker.SetActive(false);

            achievements.StateChanged += Refresh;
            Refresh();
        }

        public void Open()
        {
            if (IsVisible) return;
            previousInput = input != null && input.GameplayAllowed;
            previousLock = rig != null && rig.TransitionLocked;

            if (input != null)
            {
                input.GameplayAllowed = false;
                input.Cancel();
            }
            if (rig != null)
            {
                rig.TransitionLocked = true;
            }

            feedback = "";
            blocker.SetActive(true);
            blocker.transform.SetAsLastSibling();
            Refresh();
        }

        public void Close()
        {
            if (!IsVisible) return;
            blocker.SetActive(false);

            if (input != null)
            {
                input.GameplayAllowed = previousInput;
                input.Cancel();
            }
            if (rig != null)
            {
                rig.TransitionLocked = previousLock;
            }
            feedback = "";
        }

        public void OnClaimClicked(string achievementId)
        {
            if (achievements == null) return;
            var result = achievements.TryClaim(achievementId);
            if (result.Success)
            {
                feedback = "Nhận thành công 10 Coin vĩnh viễn!";
            }
            else
            {
                feedback = result.Reason == "ALREADY_CLAIMED" ? "Thành tựu này đã được nhận trước đó." :
                           result.Reason == "NOT_UNLOCKED" ? "Chưa đạt đủ điều kiện hoàn thành." :
                           result.Reason.StartsWith("SAVE_FAILED") ? "Chưa lưu được. Bấm để thử lại." :
                           "Không thể nhận: " + result.Reason;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (achievements == null || state == null) return;

            long coin = state.permanent != null ? state.permanent.coin : 0;
            int readyCount = 0;

            if (titleText != null)
            {
                titleText.text = string.Format(CultureInfo.InvariantCulture,
                    "THÀNH TỰU & COIN VĨNH VIỄN\nSố dư ví Coin: {0}", coin);
            }

            if (feedbackText != null)
            {
                feedbackText.text = feedback;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var status = achievements.GetStatus(row.AchievementId);

                string stateDesc = status.IsClaimed ? "<color=#2e7d32>Đã nhận thưởng</color>" :
                                   status.IsCompleted ? "<color=#e65100>Đã hoàn thành (Chưa nhận)</color>" :
                                   string.Format(CultureInfo.InvariantCulture, "<color=#555555>Tiến độ: {0}/{1}</color>", status.CurrentProgress, status.TargetProgress);

                row.InfoText.text = string.Format(CultureInfo.InvariantCulture,
                    "<b>{0}</b> (+{1} Coin)\n{2} · {3}",
                    status.DisplayName, status.CoinReward, status.Description, stateDesc);

                if (status.IsClaimed)
                {
                    row.ClaimButton.interactable = false;
                    row.ClaimButtonText.text = "Đã nhận";
                    row.ButtonImage.color = new Color(0.78f, 0.78f, 0.78f);
                }
                else if (status.CanClaim)
                {
                    readyCount++;
                    row.ClaimButton.interactable = true;
                    row.ClaimButtonText.text = "Nhận 10 Coin";
                    row.ButtonImage.color = new Color(1f, 0.84f, 0.35f); // Gold button
                }
                else
                {
                    row.ClaimButton.interactable = false;
                    row.ClaimButtonText.text = "Chưa đạt";
                    row.ButtonImage.color = new Color(0.85f, 0.88f, 0.85f);
                }
            }

            if (OpenButton != null)
            {
                var openText = OpenButton.GetComponentInChildren<Text>();
                if (openText != null)
                {
                    openText.text = readyCount > 0 ?
                        string.Format(CultureInfo.InvariantCulture, "Thành tựu (!{0})", readyCount) :
                        string.Format(CultureInfo.InvariantCulture, "Thành tựu ({0})", coin);
                }
            }
        }

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        private void LateUpdate()
        {
            if (GetComponent<ReferenceUiLayout>() != null) return;
            if (OpenButton == null || blocker == null) return;
            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) return;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            bool portrait = Screen.width < Screen.height;
            var open = (RectTransform)OpenButton.transform;
            open.anchorMin = open.anchorMax = open.pivot = new Vector2(1, 1);
            open.anchoredPosition = new Vector2(-28, -24);
            open.sizeDelta = portrait ? new Vector2(230, 60) : new Vector2(240, 60);

            float maxWidth = ((RectTransform)blocker.transform.parent).rect.width - 40;
            panel.sizeDelta = new Vector2(Mathf.Min(980, maxWidth), 840);
        }

        private void OnDestroy()
        {
            if (achievements != null) achievements.StateChanged -= Refresh;
        }

        private static Text CreateLabel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var txt = go.GetComponent<Text>();
            txt.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = new Color(0.08f, 0.23f, 0.15f);
            txt.raycastTarget = false;
            return txt;
        }

        private static Button CreateButton(string name, Transform parent, string caption, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            go.GetComponent<Image>().color = new Color(0.74f, 0.87f, 0.75f);
            var lbl = CreateLabel("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero, size, 26, FontStyle.Bold);
            lbl.text = caption;
            lbl.alignment = TextAnchor.MiddleCenter;

            return go.GetComponent<Button>();
        }
    }
}
