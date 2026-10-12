using System;
using System.Collections.Generic;
using System.Globalization;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    public sealed class ShopView : MonoBehaviour
    {
        private CosmeticSystem cosmeticSystem;
        private CosmeticView cosmeticView;
        private GameState state;
        private TreeInputAdapter input;
        private OrbitCameraRig rig;

        private GameObject blocker;
        private RectTransform panel;
        private Text titleText;
        private Text feedbackText;
        private Button closeButton;
        private readonly List<ShopItemRowUI> rows = new List<ShopItemRowUI>();

        private bool previousInput;
        private bool previousLock;
        private string feedback = "";

        public Button OpenButton { get; private set; }
        public bool IsVisible => blocker != null && blocker.activeSelf;

        private sealed class ShopItemRowUI
        {
            public string CosmeticId;
            public GameObject Root;
            public Text InfoText;
            public Button PreviewButton;
            public Text PreviewButtonText;
            public Button ActionButton; // Buy or Equip/Unequip
            public Text ActionButtonText;
            public Image ActionButtonImage;
        }

        public void Bind(CosmeticSystem cosmetics, CosmeticView visualView, GameState gameState, TreeInputAdapter adapter, OrbitCameraRig cameraRig)
        {
            cosmeticSystem = cosmetics ?? throw new ArgumentNullException(nameof(cosmetics));
            cosmeticView = visualView ?? throw new ArgumentNullException(nameof(visualView));
            state = gameState ?? throw new ArgumentNullException(nameof(gameState));
            input = adapter;
            rig = cameraRig;

            Transform safe = transform.Find("SafeArea") ?? transform;

            // 1. Nút mở Shop
            OpenButton = CreateButton("ShopOpen", safe, "Cửa hàng", new Vector2(0, 0), new Vector2(28, 28), new Vector2(230, 88));
            OpenButton.onClick.AddListener(Open);

            // 2. Blocker toàn màn hình
            blocker = new GameObject("ShopBlocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(safe, false);
            var blockerRect = (RectTransform)blocker.transform;
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            // 3. Panel trung tâm
            panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(blockerRect, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(980, 840);
            panel.GetComponent<Image>().color = new Color(0.95f, 0.94f, 0.98f);

            // 4. Tiêu đề
            titleText = CreateLabel("Title", panel, new Vector2(0, 1), new Vector2(28, -20), new Vector2(924, 70), 30, FontStyle.Bold);

            // 5. Container danh sách items
            var listContainer = new GameObject("ShopListContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            listContainer.SetParent(panel, false);
            listContainer.anchorMin = new Vector2(0, 1);
            listContainer.anchorMax = new Vector2(1, 1);
            listContainer.pivot = new Vector2(0.5f, 1);
            listContainer.anchoredPosition = new Vector2(0, -95);
            listContainer.sizeDelta = new Vector2(-56, 620);

            // Tạo các item rows
            var all = cosmeticSystem.GetAllStatuses();
            float yPos = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var def = all[i];
                var rowObj = new GameObject("ShopRow_" + def.CosmeticId, typeof(RectTransform), typeof(Image));
                rowObj.transform.SetParent(listContainer, false);
                var rowRect = (RectTransform)rowObj.transform;
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(1, 1);
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.anchoredPosition = new Vector2(0, yPos);
                rowRect.sizeDelta = new Vector2(0, 110);
                rowObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);

                var info = CreateLabel("Info", rowRect, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(480, 100), 21);
                info.alignment = TextAnchor.MiddleLeft;

                string id = def.CosmeticId;

                // Nút Preview
                var prevBtn = CreateButton("PreviewBtn", rowRect, "Xem trước", new Vector2(1, 0.5f), new Vector2(-230, 0), new Vector2(180, 64));
                prevBtn.onClick.AddListener(() => OnPreviewToggle(id));

                // Nút Mua / Trang bị
                var actionBtn = CreateButton("ActionBtn", rowRect, "Mua", new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(200, 64));
                actionBtn.onClick.AddListener(() => OnActionClicked(id));

                rows.Add(new ShopItemRowUI
                {
                    CosmeticId = id,
                    Root = rowObj,
                    InfoText = info,
                    PreviewButton = prevBtn,
                    PreviewButtonText = prevBtn.GetComponentInChildren<Text>(),
                    ActionButton = actionBtn,
                    ActionButtonText = actionBtn.GetComponentInChildren<Text>(),
                    ActionButtonImage = actionBtn.GetComponent<Image>()
                });

                yPos -= 118;
            }

            // 6. Feedback & Close
            feedbackText = CreateLabel("Feedback", panel, new Vector2(0, 0), new Vector2(28, 28), new Vector2(620, 60), 22);
            feedbackText.alignment = TextAnchor.MiddleLeft;

            closeButton = CreateButton("CloseBtn", panel, "Đóng", new Vector2(1, 0), new Vector2(-28, 28), new Vector2(240, 64));
            closeButton.onClick.AddListener(Close);

            blocker.SetActive(false);

            cosmeticSystem.StateChanged += Refresh;
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

            // Đóng panel luôn hủy bỏ mọi preview tạm thời và khôi phục trang bị thật
            if (cosmeticView != null)
            {
                cosmeticView.CancelPreview();
            }

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

        public void OnPreviewToggle(string cosmeticId)
        {
            if (cosmeticView == null) return;

            if (cosmeticView.IsPreviewing && cosmeticView.ActiveVisualId == cosmeticId)
            {
                cosmeticView.CancelPreview();
                feedback = "Đã hủy xem trước.";
            }
            else
            {
                cosmeticView.SetPreview(cosmeticId);
                feedback = "Đang xem trước trên cây (Chưa áp dụng trang bị lưu).";
            }
            Refresh();
        }

        public void OnActionClicked(string cosmeticId)
        {
            if (cosmeticSystem == null) return;

            bool owned = cosmeticSystem.IsOwned(cosmeticId);
            if (!owned)
            {
                // Hành động: Mua
                var result = cosmeticSystem.TryBuy(cosmeticId);
                if (result.Success)
                {
                    feedback = "Mua thành công! Vật phẩm đã vào túi đồ (chưa trang bị).";
                }
                else
                {
                    feedback = result.Reason == "INSUFFICIENT_COIN" ? "Không đủ Coin để mua vật phẩm này." :
                               result.Reason == "ALREADY_OWNED" ? "Bạn đã sở hữu vật phẩm này rồi." :
                               result.Reason.StartsWith("SAVE_FAILED") ? "Chưa lưu được. Bấm để thử lại." :
                               "Lỗi mua: " + result.Reason;
                }
            }
            else
            {
                // Hành động: Trang bị / Tháo
                bool equipped = cosmeticSystem.IsEquipped(cosmeticId);
                if (equipped)
                {
                    var result = cosmeticSystem.TryUnequip();
                    if (result.Success)
                    {
                        if (cosmeticView != null && cosmeticView.IsPreviewing) cosmeticView.CancelPreview();
                        feedback = "Đã tháo trang bị.";
                    }
                    else feedback = "Lỗi: " + result.Reason;
                }
                else
                {
                    var result = cosmeticSystem.TryEquip(cosmeticId);
                    if (result.Success)
                    {
                        if (cosmeticView != null && cosmeticView.IsPreviewing) cosmeticView.CancelPreview();
                        feedback = "Đã trang bị thành công (đã lưu).";
                    }
                    else feedback = "Lỗi trang bị: " + result.Reason;
                }
            }

            Refresh();
        }

        public void Refresh()
        {
            if (cosmeticSystem == null || state == null) return;

            long coin = state.permanent != null ? state.permanent.coin : 0;

            if (titleText != null)
            {
                titleText.text = string.Format(CultureInfo.InvariantCulture,
                    "CỬA HÀNG VẬT PHẨM (SHOP)\nSố dư ví Coin: {0}", coin);
            }

            if (feedbackText != null)
            {
                feedbackText.text = feedback;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var status = cosmeticSystem.GetStatus(row.CosmeticId);

                bool isThisPreviewing = cosmeticView != null && cosmeticView.IsPreviewing && cosmeticView.ActiveVisualId == row.CosmeticId;

                string stateDesc = status.IsEquipped ? "<color=#2e7d32>Đang trang bị</color>" :
                                   status.IsOwned ? "<color=#1565c0>Đã sở hữu</color>" :
                                   string.Format(CultureInfo.InvariantCulture, "<color=#555555>Giá: {0} Coin</color>", status.Price);

                if (isThisPreviewing)
                {
                    stateDesc += " · <color=#e65100>(Đang xem trước)</color>";
                }

                row.InfoText.text = string.Format(CultureInfo.InvariantCulture,
                    "<b>{0}</b> ({1} Coin)\n{2}\nTrạng thái: {3}",
                    status.DisplayName, status.Price, status.Description, stateDesc);

                // Preview button update
                row.PreviewButtonText.text = isThisPreviewing ? "Bỏ xem" : "Xem trước";
                row.PreviewButton.GetComponent<Image>().color = isThisPreviewing ? new Color(1f, 0.75f, 0.75f) : new Color(0.85f, 0.88f, 0.95f);

                // Action button update
                if (!status.IsOwned)
                {
                    row.ActionButtonText.text = string.Format(CultureInfo.InvariantCulture, "Mua ({0} Coin)", status.Price);
                    row.ActionButton.interactable = status.CanBuy;
                    row.ActionButtonImage.color = status.CanBuy ? new Color(1f, 0.84f, 0.35f) : new Color(0.85f, 0.85f, 0.85f);
                }
                else
                {
                    if (status.IsEquipped)
                    {
                        row.ActionButtonText.text = "Tháo";
                        row.ActionButton.interactable = true;
                        row.ActionButtonImage.color = new Color(0.95f, 0.8f, 0.8f);
                    }
                    else
                    {
                        row.ActionButtonText.text = "Trang bị";
                        row.ActionButton.interactable = true;
                        row.ActionButtonImage.color = new Color(0.6f, 0.9f, 0.65f);
                    }
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
            open.anchorMin = open.anchorMax = open.pivot = new Vector2(0, 0);
            open.anchoredPosition = new Vector2(28, 28);
            open.sizeDelta = new Vector2(230, 88);

            float maxWidth = ((RectTransform)blocker.transform.parent).rect.width - 40;
            panel.sizeDelta = new Vector2(Mathf.Min(980, maxWidth), 840);
        }

        private void OnDestroy()
        {
            if (cosmeticSystem != null) cosmeticSystem.StateChanged -= Refresh;
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
            txt.color = new Color(0.1f, 0.1f, 0.2f);
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

            go.GetComponent<Image>().color = new Color(0.85f, 0.88f, 0.95f);
            var lbl = CreateLabel("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero, size, 24, FontStyle.Bold);
            lbl.text = caption;
            lbl.alignment = TextAnchor.MiddleCenter;

            return go.GetComponent<Button>();
        }
    }
}
