using System;
using System.Globalization;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    [Serializable]
    public sealed class UpgradeRowView
    {
        public string branchId;
        public Button button;
        public Text details;
        public Text reason;
    }

    public sealed class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text feedback;
        [SerializeField] private UpgradeRowView[] rows;
        private UpgradeSystem upgrades;
        private ResourceSystem resources;
        private VitalitySystem vitality;
        private bool portraitLayout;
        private float lastAvailableWidth;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public RectTransform Panel => panel;
        public Button OpenButton => openButton;
        public UpgradeRowView FindRow(string id) => Array.Find(rows, row => row.branchId == id);

        public void Bind(UpgradeSystem system, ResourceSystem resourceSystem, VitalitySystem vitalitySystem)
        {
            Unsubscribe(); EnsureSpiritRow(); upgrades = system; resources = resourceSystem; vitality = vitalitySystem;
            openButton.onClick.AddListener(Toggle); closeButton.onClick.AddListener(Close);
            foreach (var row in rows) { string id = row.branchId; row.button.onClick.AddListener(() => RequestUpgrade(id)); }
            resources.Changed += Refresh; vitality.Changed += Refresh;
            panel.gameObject.SetActive(false); feedback.text = ""; Refresh();
        }
        public void Toggle() { panel.gameObject.SetActive(!IsOpen); Refresh(); GetComponent<ReferenceUiLayout>()?.Layout(); }
        public void Close() { panel.gameObject.SetActive(false); GetComponent<ReferenceUiLayout>()?.Layout(); }
        public void Layout(bool portrait)
        {
            if (GetComponent<ReferenceUiLayout>() != null) return;
            if (panel == null || openButton == null) return;
            Canvas.ForceUpdateCanvases();
            float availableWidth = ((RectTransform)panel.parent).rect.width;
            portraitLayout = portrait; lastAvailableWidth = availableWidth;
            float width = Mathf.Min(portrait ? 800 : 620, Mathf.Max(1, availableWidth - 56));
            Place((RectTransform)openButton.transform, new Vector2(1, 0), new Vector2(-28, 28), new Vector2(230, 88));
            var evolutionView = GetComponent<EvolutionPanelView>();
            Place(panel, new Vector2(1, 0), new Vector2(-28, portrait ? 270 : 128), new Vector2(width, evolutionView != null ? 860 : 650));
            if (evolutionView != null) evolutionView.Layout(portrait, width);
            ((RectTransform)feedback.transform).anchoredPosition=new Vector2(24,-580);
            feedback.fontSize=18;
            ((RectTransform)feedback.transform).sizeDelta=new Vector2(width-48,36);
            ((RectTransform)closeButton.transform).sizeDelta=new Vector2(112,48);
            foreach(var heading in panel.GetComponentsInChildren<Text>(true))
                if(heading.name=="Heading") { heading.fontSize=28; ((RectTransform)heading.transform).sizeDelta=new Vector2(width-170,40); }
            int rowIndex=0;
            openButton.GetComponentInChildren<Text>().text="Nâng cấp";
            foreach (var row in rows)
            {
                var rowRect=(RectTransform)row.details.transform.parent;
                rowRect.sizeDelta=new Vector2(-48,124);
                var rowImage=rowRect.GetComponent<Image>() ?? rowRect.gameObject.AddComponent<Image>();
                GameUiTheme.Paper(rowImage,"row_upgrade_bg"); rowImage.raycastTarget=false;
                ((RectTransform)row.details.transform.parent).anchoredPosition=new Vector2(0,-70-rowIndex++*125);
                ((RectTransform)row.button.transform).sizeDelta=new Vector2(112,72);
                row.details.fontSize = portrait ? 24 : 20;
                row.reason.fontSize = portrait ? 21 : 18;
                var rect = (RectTransform)row.details.transform;
                rect.sizeDelta = new Vector2(width - 200, 100);
                var reasonRect = (RectTransform)row.reason.transform;
                reasonRect.anchoredPosition = new Vector2(0, -100);
                reasonRect.sizeDelta = new Vector2(width - 48, 24);
            }
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
        { rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = offset; rect.sizeDelta = size; }
        private void LateUpdate()
        {
            if (panel != null && Mathf.Abs(((RectTransform)panel.parent).rect.width - lastAvailableWidth) > 0.01f) Layout(portraitLayout);
        }
        public void RequestUpgrade(string id)
        {
            if (upgrades == null) return;
            var result = upgrades.TryUpgrade(id);
            feedback.text = result.Success ? BranchLabel(id) + " +1 cấp" : ReasonText(result.Reason, upgrades.GetQuote(id).CostResourceId);
            Refresh();
        }
        public void Refresh()
        {
            if (upgrades == null) return;
            foreach (var row in rows)
            {
                var quote = upgrades.GetQuote(row.branchId);
                string output=quote.BranchId=="root"?"Water/s":quote.BranchId=="leaves"?"Quang hợp/s":quote.BranchId=="trunk"?"Growth/s":"Linh khí/s";
                string cost = ProductionSystem.Finite(quote.Cost) && quote.Cost > 0 ? Format(quote.Cost) : "—";
                row.details.text = BranchLabel(row.branchId) + "   L" + quote.Level + "/" + quote.Cap + (quote.IsUnlock?"\n":"   ·   ") + (quote.IsUnlock?Format(quote.UnlockWaterCost)+" Water · "+Format(quote.UnlockPhotosynthesisCost)+" Quang hợp · "+Format(quote.UnlockGrowthCost)+" Growth":cost+" "+ResourceLabel(quote.CostResourceId)) + "\n" +
                    output + " " + Format(quote.CurrentOutput) + " → " + Format(quote.NextOutput) +
                    (quote.BranchId=="leaves"||quote.BranchId=="trunk" ? "\nTiêu "+(quote.BranchId=="leaves"?"Water":"Quang hợp")+"/s "+Format(quote.CurrentWaterConsumption)+" → "+Format(quote.NextWaterConsumption):"");
                var buttonLabel = row.button.GetComponentInChildren<Text>();
                buttonLabel.text = quote.IsUnlock ? "Mở khóa" : cost + "\n" + ResourceLabel(quote.CostResourceId);
                buttonLabel.resizeTextForBestFit = true;
                buttonLabel.resizeTextMinSize = 14;
                buttonLabel.resizeTextMaxSize = buttonLabel.fontSize;
                row.reason.text=quote.Reason=="LOCKED"?"Mở tại Cây Trưởng Thành":quote.IsUnlock&&quote.Reason=="INSUFFICIENT_RESOURCE"?"Thiếu Water / Quang hợp / Growth":ReasonText(quote.Reason,quote.CostResourceId);
                row.button.interactable = quote.CanUpgrade;
            }
        }
        private static string BranchLabel(string id) => id=="root"?"RỄ":id=="leaves"?"LÁ":id=="trunk"?"THÂN":"HẤP THỤ LINH KHÍ";
        public static string Format(double value)
        {
            // Display only; the domain compares full precision balances/costs.
            if (value >= 1e9) return (value / 1e9).ToString("0.##", CultureInfo.InvariantCulture) + "B";
            if (value >= 1e6) return (value / 1e6).ToString("0.##", CultureInfo.InvariantCulture) + "M";
            if (value >= 1e3) return (value / 1e3).ToString("0.##", CultureInfo.InvariantCulture) + "K";
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
        public static string ResourceLabel(string resourceId) => resourceId == "water" ? "Water" : resourceId == "growth" ? "Growth" : resourceId=="photosynthesis"?"Quang hợp":resourceId=="spirit"?"Linh khí":"—";
        private static string ReasonText(string reason, string resourceId)
        {
            switch (reason)
            {
                case "": return "Sẵn sàng";
                case "LOCKED": return "Mở từ Cây Non";
                case "CAP_REACHED": return "Đã đạt cap stage";
                case "INSUFFICIENT_RESOURCE": return "Thiếu " + ResourceLabel(resourceId);
                case "DEAD": return "Cây đã chết";
                case "BUSY": return "Đang xử lý";
                default: return "Không thể nâng: " + reason;
            }
        }
        private void EnsureSpiritRow()
        {
            if(Array.Exists(rows,r=>r.branchId=="spirit_absorption"))return;
            var template=rows[rows.Length-1];
            var copy=Instantiate(template.details.transform.parent.gameObject,panel);
            copy.name="SpiritAbsorptionRow";
            var text=copy.GetComponentsInChildren<Text>(true);
            var button=copy.GetComponentInChildren<Button>(true);
            var newRow=new UpgradeRowView{branchId="spirit_absorption",button=button,
                details=Array.Find(text,t=>t.gameObject.name==template.details.gameObject.name),
                reason=Array.Find(text,t=>t.gameObject.name==template.reason.gameObject.name)};
            Array.Resize(ref rows,rows.Length+1);rows[rows.Length-1]=newRow;
        }
        private void Unsubscribe()
        {
            if (resources != null) resources.Changed -= Refresh;
            if (vitality != null) vitality.Changed -= Refresh;
            if (openButton != null) openButton.onClick.RemoveListener(Toggle);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            // Row buttons belong solely to this view; inspector persistent listeners are retained.
            if (rows != null) foreach (var row in rows) if (row.button != null) row.button.onClick.RemoveAllListeners();
        }
        private void OnDestroy() { Unsubscribe(); }
#if UNITY_EDITOR
        public void Configure(Button open, Button close, RectTransform panelRect, Text feedbackText, UpgradeRowView[] rowViews)
        { openButton = open; closeButton = close; panel = panelRect; feedback = feedbackText; rows = rowViews; }
#endif
    }
}
