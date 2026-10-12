using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace CayTheGioi.Editor
{
    public static class FullPlayModeTestRunner
    {
        [MenuItem("Cây Thế Giới/Tests/Run All PlayMode Tests")]
        public static void RunAllPlayMode()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new PlayModeCallbacks());
            var filter = new Filter()
            {
                testMode = TestMode.PlayMode
            };
            api.Execute(new ExecutionSettings(filter));
            Debug.Log("[FullPlayModeTestRunner] Triggered PlayMode tests execution.");
        }

        private sealed class PlayModeCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"[FullPlayModeTestRunner] RunStarted. Total tests: {testsToRun.TestCaseCount}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log($"[FullPlayModeTestRunner] RunFinished. Total: {result.PassCount + result.FailCount + result.SkipCount}, Passed: {result.PassCount}, Failed: {result.FailCount}, Skipped: {result.SkipCount}");

                try
                {
                    string localLow = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "DefaultCompany", "Cay_The_Gioi", "TestResults.xml");
                    string targetDir = Path.Combine(Application.dataPath, "..", "Evidence", "ReviewFix_20261012");
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    string targetPath = Path.Combine(targetDir, "PlayModeResults.xml");

                    if (File.Exists(localLow))
                    {
                        File.Copy(localLow, targetPath, true);
                        Debug.Log($"[FullPlayModeTestRunner] Copied TestResults.xml to {targetPath}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[FullPlayModeTestRunner] Error copying results: {ex.Message}");
                }
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogError($"[FullPlayModeTestRunner] FAILED: {result.FullName}: {result.Message}");
                }
            }
        }
    }
}
