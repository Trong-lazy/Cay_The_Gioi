using System.Collections;
using CayTheGioi.Bootstrap;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    public sealed class ReferenceUiSceneTests
    {
        private GameBootstrapper b;
        private ReferenceUiLayout layout;
        [UnitySetUp] public IEnumerator Setup()
        {
            SaveTestScope.Begin();yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            b=Object.FindAnyObjectByType<GameBootstrapper>();b.TickDriver.ManualTick=true;
            b.SaveCtrl.SendMessage("OnApplicationFocus",true);b.Input.SendMessage("OnApplicationFocus",true);
            foreach(var step in b.Tutorial.StepDefinitions)b.State.permanent.tutorialCompletedStepIds.Add(step.StepId);
            b.TutorialVisual.Refresh();layout=Object.FindAnyObjectByType<ReferenceUiLayout>();yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup(){SaveTestScope.End();yield return null;}
        [UnityTest]public IEnumerator ReferencePanelsDockToCorrectSidesAndNoEventIsCreated()
        {
            layout.Layout();var safe=layout.transform.Find("SafeArea");
            Assert.That(((RectTransform)safe.Find("SettingsOpen")).anchorMin.x,Is.Zero);
            Assert.That(((RectTransform)safe.Find("AchievementOpen")).anchorMin.x,Is.Zero);
            Assert.That(((RectTransform)safe.Find("ShopBlocker/ShopPanel")).anchorMin.x,Is.EqualTo(1));
            Assert.That(((RectTransform)safe.Find("SettingsBlocker/SettingsPanel")).anchorMin.x,Is.Zero);
            Assert.That(((RectTransform)safe.Find("AchievementBlocker/AchievementPanel")).anchorMin.x,Is.Zero);
            Assert.That(safe.Find("EventPanel"),Is.Null);yield return null;
        }
        [UnityTest]public IEnumerator EmptyPanelClickDoesNotCloseButOutsideClickCloses()
        {
            b.Shop.Open();layout.Layout();yield return null;
            var outside=layout.transform.Find("SafeArea/ShopBlocker").GetComponent<ReferenceOutsideClose>();
            var data=new PointerEventData(EventSystem.current);
            data.position=RectTransformUtility.WorldToScreenPoint(null,outside.Panel.TransformPoint(outside.Panel.rect.center));
            outside.OnPointerClick(data);Assert.That(b.Shop.IsVisible,Is.True);
            data.position=new Vector2(5,Screen.height*.5f);outside.OnPointerClick(data);
            Assert.That(b.Shop.IsVisible,Is.False);Assert.That(b.Input.GameplayAllowed,Is.True);yield return null;
        }
        [UnityTest]public IEnumerator VoluntaryRebirthOverviewCannotCommitBeforeConfirmation()
        {
            b.State.run.stageId="young_tree";var rebirth=Object.FindAnyObjectByType<RebirthView>();string id=b.State.run.runId;
            rebirth.Open();Assert.That(rebirth.IsConfirming,Is.False);Assert.That(rebirth.ConfirmButton.gameObject.activeSelf,Is.False);
            rebirth.BeginButton.onClick.Invoke();Assert.That(rebirth.IsConfirming,Is.True);Assert.That(b.State.run.runId,Is.EqualTo(id));
            rebirth.Cancel();Assert.That(b.State.run.runId,Is.EqualTo(id));yield return null;
        }
    }
}
