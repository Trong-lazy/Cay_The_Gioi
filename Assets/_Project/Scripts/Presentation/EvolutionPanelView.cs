using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    public sealed class EvolutionPanelView : MonoBehaviour
    {
        [SerializeField] private Text details, reason;
        [SerializeField] private Button evolve, orbitLock;
        private EvolutionSystem evolution;
        private ResourceSystem resources;
        private GameState state;
        private OrbitCameraRig rig;
        public Button EvolveButton => evolve;
        public Button OrbitButton => orbitLock;
        public string Details => details.text;
        public string Reason => reason.text;
        public void Bind(EvolutionSystem system, ResourceSystem resourceSystem, GameState game, OrbitCameraRig camera)
        {
            Unsubscribe(); evolution = system; resources = resourceSystem; state = game; rig = camera;
            evolve.GetComponentInChildren<Text>().text="Tiến hóa";
            evolve.onClick.AddListener(RequestEvolution); orbitLock.onClick.AddListener(ToggleOrbit);
            resources.Changed += Refresh; evolution.TransitionCompleted += Refresh; Refresh();
        }
        public void RequestEvolution() { var result = evolution.TryEvolve(); Refresh(); if (!result.Success) reason.text = ReasonText(result.Reason); }
        public void ToggleOrbit()
        {
            if (evolution.IsTransitioning) return;
            state.settings.orbitLocked = !state.settings.orbitLocked; rig.OrbitLocked = state.settings.orbitLocked; Refresh();
        }
        public void Refresh()
        {
            if (evolution == null) return;
            var quote = evolution.GetQuote();
            details.text = quote.CurrentName + " → " + (quote.NextName ?? "Stage cuối") + "\n" +
                (quote.NextId == null ? "Không còn Evolution" : UpgradePanelView.Format(quote.WaterCost) + " Water · " + UpgradePanelView.Format(quote.PhotosynthesisCost)+" Quang hợp · "+UpgradePanelView.Format(quote.GrowthCost) + " Growth");
            reason.text = ReasonText(quote.Reason); evolve.interactable = quote.CanEvolve;
            orbitLock.interactable = !evolution.IsTransitioning;
            orbitLock.GetComponentInChildren<Text>().text = state.settings.orbitLocked ? "Orbit: Đã khóa" : "Orbit: Tự do";
        }
        private static string ReasonText(string id)
        {
            switch(id) { case "": return "Giữ level, trừ chi phí; không nhận point"; case "BUSY": return "Đang chuyển stage";
                case "DEAD": return "Cây đã chết"; case "FINAL_STAGE": return "Đã đạt stage cuối";
                case "INSUFFICIENT_RESOURCE": return "Thiếu Water, Quang hợp hoặc Growth"; default: return "Không thể Evolution: " + id; }
        }
        public void Layout(bool portrait, float width)
        {
            if (details == null) return;
            ((RectTransform)details.transform).anchoredPosition=new Vector2(24,-635);
            ((RectTransform)reason.transform).anchoredPosition=new Vector2(24,-740);
            ((RectTransform)evolve.transform).anchoredPosition=new Vector2(-24,-635);
            if (orbitLock != null && orbitLock.transform.parent == evolve.transform.parent)
            {
                ((RectTransform)orbitLock.transform).anchoredPosition=new Vector2(24,-785);
                ((RectTransform)orbitLock.transform).sizeDelta=new Vector2(230,56);
            }
            ((RectTransform)evolve.transform).sizeDelta=new Vector2(112,72);
            details.fontSize = portrait ? 25 : 20; reason.fontSize = portrait ? 23 : 18;
            ((RectTransform)details.transform).sizeDelta = new Vector2(width - 200, 100);
            ((RectTransform)reason.transform).sizeDelta = new Vector2(width - 48, 40);
        }
        private void Unsubscribe()
        {
            if (resources != null) resources.Changed -= Refresh;
            if (evolution != null) evolution.TransitionCompleted -= Refresh;
            if (evolve != null) evolve.onClick.RemoveListener(RequestEvolution);
            if (orbitLock != null) orbitLock.onClick.RemoveListener(ToggleOrbit);
        }
        private void OnDestroy() { Unsubscribe(); }
#if UNITY_EDITOR
        public void Configure(Text information, Text status, Button action, Button orbit)
        { details = information; reason = status; evolve = action; orbitLock = orbit; }
#endif
    }
}
