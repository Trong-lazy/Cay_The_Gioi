using CayTheGioi.Bootstrap;
using CayTheGioi.Presentation;
using UnityEditor;
using UnityEngine;

namespace CayTheGioi.Editor
{
    public static class AchievementDebug
    {
        [MenuItem("Cây Thế Giới/T08A/Debug/Log Status")]
        public static void LogStatus()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            if (b == null || !b.IsInitialized || b.Achievements == null)
            {
                Debug.LogWarning("[T08A Debug] Bootstrapper or AchievementSystem not ready.");
                return;
            }

            var all = b.Achievements.GetAllStatuses();
            Debug.Log($"[T08A Debug] Coin = {b.State.permanent.coin}. Achievements count = {all.Count}:");
            foreach (var a in all)
            {
                Debug.Log($" - [{a.AchievementId}] {a.DisplayName}: progress={a.CurrentProgress}/{a.TargetProgress}, completed={a.IsCompleted}, claimed={a.IsClaimed}, canClaim={a.CanClaim}");
            }
        }

        [MenuItem("Cây Thế Giới/T08A/Debug/Log Status", true)]
        public static bool CanLogStatus() => EditorApplication.isPlaying;

        [MenuItem("Cây Thế Giới/T08A/Debug/Grant All Progress")]
        public static void GrantAllProgress()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            if (b == null || !b.IsInitialized || b.Achievements == null) return;

            var all = b.Achievements.GetAllStatuses();
            foreach (var a in all)
            {
                b.Achievements.TryUpdateProgress(a.AchievementId, a.TargetProgress);
            }
            Debug.Log("[T08A Debug] Granted target progress to all achievements.");
        }

        [MenuItem("Cây Thế Giới/T08A/Debug/Grant All Progress", true)]
        public static bool CanGrantAllProgress() => EditorApplication.isPlaying;

        [MenuItem("Cây Thế Giới/T08A/Debug/Claim All Available")]
        public static void ClaimAllAvailable()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            if (b == null || !b.IsInitialized || b.Achievements == null) return;

            var all = b.Achievements.GetAllStatuses();
            int claimed = 0;
            foreach (var a in all)
            {
                if (a.CanClaim)
                {
                    var res = b.Achievements.TryClaim(a.AchievementId);
                    if (res.Success) claimed++;
                }
            }
            Debug.Log($"[T08A Debug] Claimed {claimed} achievements. New coin balance = {b.State.permanent.coin}");
        }

        [MenuItem("Cây Thế Giới/T08A/Debug/Claim All Available", true)]
        public static bool CanClaimAllAvailable() => EditorApplication.isPlaying;

        [MenuItem("Cây Thế Giới/T08A/Debug/Open Panel")]
        public static void OpenPanel()
        {
            var view = Object.FindAnyObjectByType<AchievementView>();
            if (view != null) view.Open();
        }

        [MenuItem("Cây Thế Giới/T08A/Debug/Open Panel", true)]
        public static bool CanOpenPanel() => EditorApplication.isPlaying;
    }
}
