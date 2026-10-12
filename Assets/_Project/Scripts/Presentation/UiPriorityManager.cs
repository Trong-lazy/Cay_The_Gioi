using System;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class UiPriorityManager : MonoBehaviour
    {
        private GameState state;
        private RebirthView rebirthView;
        private SettingsView settingsView;
        private ShopView shopView;
        private AchievementView achievementView;
        private UpgradePanelView upgradePanelView;

        public void Bind(
            GameState gameState,
            RebirthView rebirth,
            SettingsView settings,
            ShopView shop,
            AchievementView achievements,
            UpgradePanelView upgrades)
        {
            state = gameState ?? throw new ArgumentNullException(nameof(gameState));
            rebirthView = rebirth;
            settingsView = settings;
            shopView = shop;
            achievementView = achievements;
            upgradePanelView = upgrades;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }
        }

        public void HandleEscape()
        {
            if (state == null) return;

            // 1. DEAD modal có ưu tiên cao nhất: không cho Escape thoát modal bắt buộc Rebirth
            if (state.run.life == "DEAD")
            {
                return;
            }

            // 2. Settings modal
            if (settingsView != null && settingsView.IsVisible)
            {
                settingsView.Close();
                return;
            }

            // 3. Shop modal
            if (shopView != null && shopView.IsVisible)
            {
                shopView.Close();
                return;
            }

            // 4. Achievement modal
            if (achievementView != null && achievementView.IsVisible)
            {
                achievementView.Close();
                return;
            }

            // 5. Rebirth modal (khi ALIVE)
            if (rebirthView != null && rebirthView.IsVisible)
            {
                rebirthView.Cancel();
                return;
            }

            // 6. Upgrade panel
            if (upgradePanelView != null && upgradePanelView.IsOpen)
            {
                upgradePanelView.Close();
                return;
            }

            // 7. Không có panel nào mở -> Mở Settings
            if (settingsView != null && !settingsView.IsVisible)
            {
                settingsView.Open();
            }
        }
    }
}
