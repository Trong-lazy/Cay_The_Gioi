using System.Collections;
using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    public sealed class FoundationSceneTests
    {
        [UnitySetUp] public IEnumerator Setup() { SaveTestScope.Begin(); yield return null; }
        [UnityTearDown] public IEnumerator Cleanup() { SaveTestScope.End(); yield return null; }
        [UnityTest]
        public IEnumerator MainSceneInitializesWithoutDuplicatingOnReload()
        {
            string priorRun = null;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Delete saves each cycle so load outcome is always NewGame.
                new SaveService(new PrototypeRules()).DeleteSaveFiles();
                var version = new CayTheGioi.Domain.PrototypeRules().configVersion;
                LogAssert.Expect(LogType.Log, "[CayTheGioi] T07 initialized: " + version + ", stage=seedling, loadOutcome=NewGame");
                yield return SceneManager.LoadSceneAsync("MainScene");
                yield return null;
                var all = Object.FindObjectsByType<GameBootstrapper>();
                Assert.That(all.Length, Is.EqualTo(1));
                var bootstrap = all[0];
                Assert.That(bootstrap.IsInitialized, Is.True);
                Assert.That(bootstrap.State.run.stageId, Is.EqualTo("seedling"));
                Assert.That(bootstrap.State.run.water, Is.EqualTo(10));
                Assert.That(bootstrap.State.run.runId, Is.Not.EqualTo(priorRun));
                priorRun = bootstrap.State.run.runId;
                var sameState = bootstrap.State;
                Assert.That(bootstrap.TryInitialize(out string error), Is.True, error);
                Assert.That(bootstrap.State, Is.SameAs(sameState));
                Assert.That(Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<AudioListener>().Length, Is.EqualTo(1));
                var tree = Object.FindAnyObjectByType<TreeView>();
                Assert.That(tree.CurrentStageId, Is.EqualTo("seedling"));
                Assert.That(tree.ModelSlot.childCount, Is.EqualTo(1));
                Assert.That(tree.RootAnchor, Is.Not.Null);
                Assert.That(tree.LeavesAnchor, Is.Not.Null);
                Assert.That(tree.TrunkAnchor, Is.Not.Null);
                Assert.That(Camera.main, Is.Not.Null);
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
