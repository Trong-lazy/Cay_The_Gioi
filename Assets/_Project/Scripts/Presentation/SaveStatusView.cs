using CayTheGioi.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    // Functional UI only. Commands delegate to the save layer; this view never edits state.
    public sealed class SaveStatusView : MonoBehaviour
    {
        private GameBootstrapper bootstrap;
        private Text status, message;
        private RectTransform modal;
        private GameObject blocker;
        private bool resetConfirmed;
        public Button RetryButton { get; private set; }
        public Button RecoveryButton { get; private set; }
        public Button ResetButton { get; private set; }
        public Button RetrySaveButton { get; private set; }
        public bool LoadPanelVisible => blocker != null && blocker.activeSelf;
        public string StatusText => status == null ? "" : status.text;

        public void Bind(GameBootstrapper value)
        {
            bootstrap = value;
            if (status != null) return;
            var safe = transform.Find("SafeArea");
            if (safe == null) safe = transform;
            status = Label("SaveStatus", safe, new Vector2(0, 1), new Vector2(28, -398), new Vector2(740, 60), 25);
            RetrySaveButton = MakeButton("RetrySave", safe, "Lưu lại", new Vector2(0, 1), new Vector2(28, -458), new Vector2(220, 72));
            RetrySaveButton.onClick.AddListener(() => bootstrap.SaveCtrl.ForceSave("UI_RETRY"));
            blocker = new GameObject("SaveLoadBlocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(safe, false);
            var full = (RectTransform)blocker.transform;
            full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one; full.offsetMin = full.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            modal = new GameObject("SaveLoadPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            modal.SetParent(full, false); modal.anchorMin = modal.anchorMax = modal.pivot = new Vector2(0.5f, 0.5f);
            modal.sizeDelta = new Vector2(900, 400); modal.GetComponent<Image>().color = new Color(0.91f, 0.96f, 0.9f);
            message = Label("Message", modal, new Vector2(0, 1), new Vector2(28, -24), new Vector2(844, 246), 28);
            RetryButton = MakeButton("RetryLoad", modal, "Thử lại", new Vector2(0, 0), new Vector2(28, 28), new Vector2(250, 88));
            RecoveryButton = MakeButton("RecoverBackup", modal, "Phục hồi backup", new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(290, 88));
            ResetButton = MakeButton("ResetSave", modal, "Bắt đầu lại", new Vector2(1, 0), new Vector2(-28, 28), new Vector2(250, 88));
            RetryButton.onClick.AddListener(() => { resetConfirmed = false; if (bootstrap.IsInitialized) bootstrap.SaveCtrl.RetryOffline(); else bootstrap.RetryLoad(); Refresh(); });
            RecoveryButton.onClick.AddListener(() => { resetConfirmed = false; bootstrap.RetryLoad(true); Refresh(); });
            ResetButton.onClick.AddListener(() =>
            {
                if (!resetConfirmed)
                { resetConfirmed = true; message.text = "Bắt đầu game mới? Save hiện tại sẽ được giữ trong file lưu trữ .reset. Bấm ‘Xác nhận’ để tiếp tục."; ResetButton.GetComponentInChildren<Text>().text = "Xác nhận"; return; }
                var result = bootstrap.ArchiveAndReset(); resetConfirmed = false; Refresh();
                if (!result.Success) message.text = "Chưa thể lưu trữ save cũ. Hãy thử lại.\n" + result.Reason;
            });
            Refresh();
        }

        private void LateUpdate()
        {
            if (modal == null) return;
            bool portrait=Screen.width<Screen.height;
            bool reference=GetComponent<ReferenceUiLayout>()!=null;
            GameUiTheme.Place((RectTransform)status.transform,new Vector2(0,1),new Vector2(28,reference?-455:portrait?-440:-330),new Vector2(490,48));
            status.fontSize=18;
            GameUiTheme.Place((RectTransform)RetrySaveButton.transform,new Vector2(0,1),new Vector2(28,reference?-520:portrait?-492:-382),new Vector2(220,72));
            var parent = (RectTransform)blocker.transform;
            modal.sizeDelta = new Vector2(Mathf.Min(900, Mathf.Max(500, parent.rect.width - 40)), 400);
            ((RectTransform)message.transform).sizeDelta = new Vector2(modal.rect.width - 56, 246);
        }

        public void Refresh()
        {
            if (bootstrap == null || status == null) return;
            bool ready = bootstrap.IsInitialized;
            bool pending = ready && bootstrap.SaveCtrl != null && bootstrap.SaveCtrl.AwaitingOffline && !bootstrap.SaveCtrl.Suspended;
            blocker.SetActive(pending || (!ready && !string.IsNullOrEmpty(bootstrap.LoadError)));
            bool dirty = ready && bootstrap.SaveCtrl != null && bootstrap.SaveCtrl.HasDirtySave;
            RetrySaveButton.gameObject.SetActive(dirty && !pending);
            status.gameObject.SetActive(dirty || !ready);
            status.text = dirty ? "Chưa lưu được tiến độ. Bấm Lưu lại." : ready ? "Lưu tự động khi tạm dừng hoặc thoát." : "Đang chờ tải save.";
            RecoveryButton.gameObject.SetActive(!ready && bootstrap.LastLoadOutcome == Infrastructure.SaveService.LoadOutcome.LoadedFromBackup);
            ResetButton.GetComponentInChildren<Text>().text = "Bắt đầu lại";
            ResetButton.gameObject.SetActive(!ready && (bootstrap.LoadError == null || !bootstrap.LoadError.StartsWith("OFFLINE")));
            if (pending) message.text = "Chưa lưu được thưởng offline. Game tạm dừng để giữ tiến độ. Bấm Thử lại để tiếp tục.";
            else if (!ready)
            {
                bool recovery = bootstrap.LastLoadOutcome == Infrastructure.SaveService.LoadOutcome.LoadedFromBackup;
                message.text = recovery
                    ? "Save chính bị lỗi. Có backup hợp lệ nhưng có thể mất tiến độ gần nhất. Xác nhận phục hồi để tiếp tục; không nhận thưởng từ thời gian cũ."
                    : bootstrap.LoadError != null && bootstrap.LoadError.StartsWith("OFFLINE") ? "Chưa lưu được thưởng offline. File được giữ nguyên; bấm Thử lại để tiếp tục.\n" + bootstrap.LoadError
                    : "Chưa thể tải save. File được giữ nguyên. Bạn có thể thử lại hoặc lưu trữ save cũ rồi bắt đầu lại.\n" + bootstrap.LoadError;
            }
        }

        private static Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int font)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>(); text.transform.SetParent(parent, false);
            var rect = (RectTransform)text.transform; rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = font; text.color = new Color(0.08f, 0.23f, 0.15f); text.raycastTarget = false;
            return text;
        }
        private static Button MakeButton(string name, Transform parent, string caption, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>(); button.transform.SetParent(parent, false);
            var rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            button.GetComponent<Image>().color = new Color(0.74f, 0.87f, 0.75f);
            var label = Label("Label", button.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size, 27); label.text = caption; label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
