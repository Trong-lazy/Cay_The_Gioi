#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace CayTheGioi.Tests
{
    // Explicit local QA requests only; never starts tests on a normal reload.
    [InitializeOnLoad]
    internal static class ReviewTestRunner
    {
        private const string DirectoryPath = "Evidence/ReviewFix_20261012";
        private static readonly TestRunnerApi api;
        static ReviewTestRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
            EditorApplication.update += Poll;
        }
        private static void Poll()
        {
            string request = DirectoryPath + "/test-request.txt";
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            string[] parts = File.ReadAllText(request).Trim().Split(':');
            string mode = parts[0];
            File.Delete(request);
            if (mode == "BuildEnvironment")
            {
                CayTheGioi.Editor.TropicalEnvironmentSetup.SetupEnvironment();
                File.WriteAllText(DirectoryPath + "/environment-build.txt", System.DateTime.UtcNow.ToString("O"));
                return;
            }
            if (mode != "EditMode" && mode != "PlayMode") return;
            SessionState.SetString("WorldTree.ReviewTestMode", mode);
            api.Execute(new ExecutionSettings(new Filter { testMode = mode == "EditMode" ? TestMode.EditMode : TestMode.PlayMode, groupNames = parts.Length > 1 && parts[1].Length > 0 ? new[] { parts[1] } : null, testNames = parts.Length > 2 ? new[] { parts[2] } : null }));
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string mode = SessionState.GetString("WorldTree.ReviewTestMode", "");
                if (mode.Length == 0) return;
                Directory.CreateDirectory(DirectoryPath);
                TestRunnerApi.SaveResultToFile(result, DirectoryPath + "/" + mode + "Results.xml");
                SessionState.EraseString("WorldTree.ReviewTestMode");
            }
        }
    }
}
#endif
