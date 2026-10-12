using CayTheGioi.Bootstrap;
using CayTheGioi.Presentation;
using UnityEditor;
using UnityEngine;

namespace CayTheGioi.Editor
{
    public static class ShopDebug
    {
        [MenuItem("Cây Thế Giới/T08B/Debug/Log Shop Status")]
        public static void LogStatus()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            if (b == null || !b.IsInitialized || b.Cosmetics == null)
            {
                Debug.LogWarning("[T08B Debug] Bootstrapper or CosmeticSystem not ready.");
                return;
            }

            var all = b.Cosmetics.GetAllStatuses();
            Debug.Log($"[T08B Debug] Coin = {b.State.permanent.coin}, Equipped = {b.Cosmetics.EquippedCosmeticId}:");
            foreach (var item in all)
            {
                Debug.Log($" - [{item.CosmeticId}] {item.DisplayName} ({item.Price} Coin): owned={item.IsOwned}, equipped={item.IsEquipped}, canBuy={item.CanBuy}");
            }
        }

        [MenuItem("Cây Thế Giới/T08B/Debug/Log Shop Status", true)]
        public static bool CanLogStatus() => EditorApplication.isPlaying;

        [MenuItem("Cây Thế Giới/T08B/Debug/Add 50 Coin (QA Only)")]
        public static void Add50Coin()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            if (b == null || !b.IsInitialized) return;

            b.State.permanent.coin += 50;
            b.SaveCtrl.ForceSave("T08B_DEBUG_ADD_COIN");
            Debug.Log($"[T08B Debug] Added 50 Coin. New balance = {b.State.permanent.coin}");
        }

        [MenuItem("Cây Thế Giới/T08B/Debug/Add 50 Coin (QA Only)", true)]
        public static bool CanAdd50Coin() => EditorApplication.isPlaying;

        [MenuItem("Cây Thế Giới/T08B/Debug/Open Shop Panel")]
        public static void OpenShopPanel()
        {
            var shop = Object.FindAnyObjectByType<ShopView>();
            if (shop != null) shop.Open();
        }

        [MenuItem("Cây Thế Giới/T08B/Debug/Open Shop Panel", true)]
        public static bool CanOpenShopPanel() => EditorApplication.isPlaying;

        [MenuItem("Cây Thế Giới/Focus Unity Window")]
        public static void FocusEditor()
        {
            var type = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (type != null)
            {
                var window = UnityEditor.EditorWindow.GetWindow(type);
                if (window != null)
                {
                    window.Focus();
                    window.Show();
                }
            }
        }
    }
}
