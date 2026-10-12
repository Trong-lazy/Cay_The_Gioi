using System;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SettingsView : MonoBehaviour
    {
        private GameState state;
        private AudioController audioController;
        private OrbitCameraRig cameraRig;
        private TreeInputAdapter input;
        private Action<string> onSaveRequested;
        private ToastView toastView;

        private GameObject blocker;
        private RectTransform panel;
        private Button musicButton;
        private Text musicButtonText;
        private Button orbitButton;
        private Text orbitButtonText;
        private Button continueButton;
        private Button exitButton;

        private bool previousInput;
        private bool previousLock;

        public Button OpenButton { get; private set; }
        public bool IsVisible => blocker != null && blocker.activeSelf;

        public void Bind(GameState gameState, AudioController audioCtrl, OrbitCameraRig rig, TreeInputAdapter adapter, Action<string> saveCallback, ToastView toast)
        {
            state = gameState ?? throw new ArgumentNullException(nameof(gameState));
            audioController = audioCtrl;
            cameraRig = rig;
            input = adapter;
            onSaveRequested = saveCallback;
            toastView = toast;

            Transform safe = transform.Find("SafeArea") ?? transform;

            // 1. Nút mở Settings
            OpenButton = CreateButton("SettingsOpen", safe, "Cài đặt", new Vector2(1, 1), new Vector2(-272, -24), new Vector2(240, 60));
            OpenButton.onClick.AddListener(Open);

            // 2. Blocker
            blocker = new GameObject("SettingsBlocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(safe, false);
            var blockerRect = (RectTransform)blocker.transform;
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            // 3. Panel
            panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(blockerRect, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(720, 600);
            panel.GetComponent<Image>().color = new Color(0.93f, 0.95f, 0.98f);

            // 4. Header
            CreateLabel("SettingsTitle", panel, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(600, 50), 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            var title = panel.Find("SettingsTitle").GetComponent<Text>();
            title.text = "CÀI ĐẶT";
            title.color = new Color(0.12f, 0.16f, 0.22f);

            // 5. Music Row
            CreateLabel("MusicLabel", panel, new Vector2(0, 1), new Vector2(60, -110), new Vector2(300, 40), 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            panel.Find("MusicLabel").GetComponent<Text>().text = "Âm nhạc nền (BGM):";

            musicButton = CreateButton("MusicToggleBtn", panel, "Bật", new Vector2(1, 1), new Vector2(-60, -110), new Vector2(180, 50));
            musicButtonText = musicButton.GetComponentInChildren<Text>();
            musicButton.onClick.AddListener(ToggleMusic);

            // 6. Orbit Lock Row
            CreateLabel("OrbitLabel", panel, new Vector2(0, 1), new Vector2(60, -190), new Vector2(300, 40), 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            panel.Find("OrbitLabel").GetComponent<Text>().text = "Khóa xoay camera (Orbit):";

            orbitButton = CreateButton("OrbitToggleBtn", panel, "Tắt", new Vector2(1, 1), new Vector2(-60, -190), new Vector2(180, 50));
            orbitButtonText = orbitButton.GetComponentInChildren<Text>();
            orbitButton.onClick.AddListener(ToggleOrbit);

            // 7. Zoom Note (Coming soon)
            CreateLabel("ZoomNote", panel, new Vector2(0.5f, 1), new Vector2(0, -270), new Vector2(600, 40), 18, FontStyle.Italic, TextAnchor.MiddleCenter);
            var zoomTxt = panel.Find("ZoomNote").GetComponent<Text>();
            zoomTxt.text = "Zoom camera: Tính năng sắp ra mắt (Coming Soon)";
            zoomTxt.color = new Color(0.45f, 0.5f, 0.55f);

            // 8. Buttons
            continueButton = CreateButton("ContinueBtn", panel, "Tiếp tục chơi", new Vector2(0.5f, 0), new Vector2(-150, 60), new Vector2(240, 54));
            continueButton.onClick.AddListener(Close);

            exitButton = CreateButton("ExitBtn", panel, "Thoát game", new Vector2(0.5f, 0), new Vector2(150, 60), new Vector2(240, 54));
            exitButton.GetComponent<Image>().color = new Color(0.85f, 0.35f, 0.35f);
            exitButton.onClick.AddListener(OnExitGame);

            blocker.SetActive(false);
            Refresh();
        }

        public void Open()
        {
            if (blocker == null || state == null) return;
            if (state.run.life == "DEAD") return; // DEAD modal overrides everything

            if (input != null)
            {
                previousInput = input.GameplayAllowed;
                input.GameplayAllowed = false;
            }
            if (cameraRig != null)
            {
                previousLock = cameraRig.OrbitLocked;
                cameraRig.OrbitLocked = true;
            }

            blocker.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (blocker == null) return;
            blocker.SetActive(false);

            if (input != null)
            {
                input.GameplayAllowed = previousInput;
            }
            if (cameraRig != null && state != null && state.settings != null)
            {
                cameraRig.OrbitLocked = state.settings.orbitLocked;
            }
        }

        public void ToggleMusic()
        {
            if (state == null || state.settings == null) return;
            state.settings.musicEnabled = !state.settings.musicEnabled;

            if (audioController != null)
            {
                audioController.SetMusicEnabled(state.settings.musicEnabled);
            }

            onSaveRequested?.Invoke("SETTINGS_MUSIC");

            if (toastView != null)
            {
                toastView.Show(state.settings.musicEnabled ? "Âm nhạc: BẬT" : "Âm nhạc: TẮT");
            }

            Refresh();
        }

        public void ToggleOrbit()
        {
            if (state == null || state.settings == null) return;
            state.settings.orbitLocked = !state.settings.orbitLocked;

            onSaveRequested?.Invoke("SETTINGS_ORBIT");

            if (toastView != null)
            {
                toastView.Show(state.settings.orbitLocked ? "Đã khóa xoay camera" : "Đã mở khóa xoay camera");
            }

            Refresh();
        }

        private void OnExitGame()
        {
            Debug.Log("[CayTheGioi] Exit game requested.");
            onSaveRequested?.Invoke("EXIT_REQUESTED");
#if UNITY_EDITOR
            // In Editor play mode, log exit intention
            Debug.Log("[CayTheGioi] Editor: Game exit simulation.");
#else
            Application.Quit();
#endif
        }

        public void Refresh()
        {
            if (state == null || state.settings == null) return;

            if (musicButtonText != null)
            {
                musicButtonText.text = state.settings.musicEnabled ? "BẬT" : "TẮT";
                musicButton.GetComponent<Image>().color = state.settings.musicEnabled ? new Color(0.35f, 0.75f, 0.45f) : new Color(0.6f, 0.65f, 0.7f);
            }

            if (orbitButtonText != null)
            {
                orbitButtonText.text = state.settings.orbitLocked ? "ĐÃ KHÓA" : "MỞ KHÓA";
                orbitButton.GetComponent<Image>().color = state.settings.orbitLocked ? new Color(0.85f, 0.55f, 0.25f) : new Color(0.35f, 0.75f, 0.45f);
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
            open.anchoredPosition = portrait ? new Vector2(-28, -164) : new Vector2(-272, -24);
            open.sizeDelta = portrait ? new Vector2(230, 60) : new Vector2(240, 60);

            float maxWidth = ((RectTransform)blocker.transform.parent).rect.width - 40;
            panel.sizeDelta = new Vector2(Mathf.Min(720, maxWidth), 600);
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            go.GetComponent<Image>().color = new Color(0.24f, 0.56f, 0.36f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            var txt = textGo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;

            return go.GetComponent<Button>();
        }

        private static Text CreateLabel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = alignment;
            txt.color = new Color(0.15f, 0.18f, 0.22f);
            return txt;
        }
    }
}
