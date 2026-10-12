using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace CayTheGioi.Editor
{
    /// <summary>
    /// Editor debug menu for T04 Save System.
    /// All commands are runtime-only and affect only in-memory state or disk files.
    /// They do NOT mutate config assets, GUID, meta or scene assets.
    /// </summary>
    public static class SaveSetup
    {
        private const string Menu = "Cây Thế Giới/T04 Save/Debug/";

        // ── Force save ─────────────────────────────────────────────────

        [MenuItem(Menu + "Force Save Now")]
        public static void ForceSaveNow()
        {
            var ctrl = GetController();
            var result = ctrl.ForceSave("T04_EDITOR_FORCE");
            // Avoid nested quotes inside string interpolation (C# < 11)
            string status = result.Success ? "OK rev=" + ctrl.Revision : "FAILED: " + result.Reason;
            Debug.Log("[T04 Debug] Force save " + status);
        }

        [MenuItem(Menu + "Force Save Now", validate = true)]
        public static bool ForceSaveNowValidate() => EditorApplication.isPlaying;

        // ── Log path ────────────────────────────────────────────────────

        [MenuItem(Menu + "Log Save File Path")]
        public static void LogSavePath()
        {
            var b = Bootstrap();
            bool mainExists = File.Exists(b.SaveSvc.MainPath);
            bool backExists = File.Exists(b.SaveSvc.BackupPath);
            Debug.Log("[T04 Debug] SavePath=" + b.SaveSvc.MainPath + " exists=" + mainExists);
            Debug.Log("[T04 Debug] BackupPath=" + b.SaveSvc.BackupPath + " exists=" + backExists);
            if (mainExists)
            {
                string json = File.ReadAllText(b.SaveSvc.MainPath);
                int len = System.Math.Min(500, json.Length);
                Debug.Log("[T04 Debug] main content (first 500 chars): " + json.Substring(0, len));
            }
        }

        [MenuItem(Menu + "Log Save File Path", validate = true)]
        public static bool LogSavePathValidate() => EditorApplication.isPlaying;

        // Fault tools only operate on an explicitly isolated validation session.
        [MenuItem(Menu + "Corrupt Main (Validation Only)")]
        public static void CorruptMainSave()
        {
            if (string.IsNullOrEmpty(SaveService.ValidationDirectory)) throw new System.InvalidOperationException("Use an isolated validation session first.");
            File.WriteAllText(Bootstrap().SaveSvc.MainPath, "{invalid json");
        }

        [MenuItem("Cây Thế Giới/T04 Save/Prepare Save UI")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play first.");
            var hud = Object.FindAnyObjectByType<CayTheGioi.Presentation.FoundationHudView>();
            if (hud.GetComponent<CayTheGioi.Presentation.SaveStatusView>() == null) hud.gameObject.AddComponent<CayTheGioi.Presentation.SaveStatusView>();
            var scene = hud.gameObject.scene;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static GameBootstrapper Bootstrap()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            if (!EditorApplication.isPlaying || b == null || !b.IsInitialized)
                throw new System.InvalidOperationException("Enter MainScene Play first.");
            return b;
        }

        private static SaveController GetController()
        {
            var b = Bootstrap();
            var ctrl = b.SaveCtrl;
            if (ctrl == null) throw new System.InvalidOperationException("SaveController not found on bootstrapper.");
            return ctrl;
        }
    }
}
