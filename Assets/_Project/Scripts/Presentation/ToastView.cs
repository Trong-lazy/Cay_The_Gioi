using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ToastView : MonoBehaviour
    {
        private readonly Queue<string> messageQueue = new Queue<string>();
        private GameObject toastContainer;
        private CanvasGroup canvasGroup;
        private Text toastText;
        private Image toastBg;

        private float displayTimer;
        private const float DisplayDuration = 2.5f;
        private const float FadeDuration = 0.3f;
        private bool isShowing;

        public bool IsShowing => isShowing;
        public string CurrentMessage => toastText != null ? toastText.text : "";

        public void Bind()
        {
            if (toastContainer != null) return;

            Transform safe = transform.Find("SafeArea") ?? transform;

            toastContainer = new GameObject("ToastContainer", typeof(RectTransform), typeof(CanvasGroup));
            toastContainer.transform.SetParent(safe, false);

            var rect = (RectTransform)toastContainer.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -340);
            rect.sizeDelta = new Vector2(560, 60);

            canvasGroup = toastContainer.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false; // Toast tuyệt đối không chặn raycast
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0f;

            // Background
            var bgGo = new GameObject("ToastBg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(toastContainer.transform, false);
            var bgRect = (RectTransform)bgGo.transform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            toastBg = bgGo.GetComponent<Image>();
            toastBg.color = new Color(0.12f, 0.15f, 0.18f, 0.88f);
            toastBg.raycastTarget = false; // Không nhận raycast

            // Text
            var textGo = new GameObject("ToastText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(toastContainer.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 8);
            textRect.offsetMax = new Vector2(-16, -8);

            toastText = textGo.GetComponent<Text>();
            toastText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toastText.fontSize = 20;
            toastText.alignment = TextAnchor.MiddleCenter;
            toastText.color = Color.white;
            toastText.raycastTarget = false; // Không nhận raycast
        }

        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            Bind();

            messageQueue.Enqueue(message);
            if (!isShowing)
            {
                DisplayNextMessage();
            }
        }

        private void DisplayNextMessage()
        {
            if (messageQueue.Count == 0)
            {
                isShowing = false;
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                return;
            }

            isShowing = true;
            string msg = messageQueue.Dequeue();
            if (toastText != null) toastText.text = msg;
            displayTimer = DisplayDuration;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (!isShowing || canvasGroup == null) return;

            displayTimer -= Time.unscaledDeltaTime;
            if (displayTimer <= 0f)
            {
                DisplayNextMessage();
            }
            else if (displayTimer < FadeDuration)
            {
                canvasGroup.alpha = displayTimer / FadeDuration;
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }
    }
}
