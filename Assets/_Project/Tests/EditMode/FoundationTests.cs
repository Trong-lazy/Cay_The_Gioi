using System;
using CayTheGioi.Configuration;
using CayTheGioi.Domain;
using CayTheGioi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class FoundationTests
    {
        [Test]
        public void DefaultRulesMatchConfirmedOfflineAndVitalityContracts()
        {
            var rules = new PrototypeRules();
            Assert.That(rules.Validate(), Is.Empty);
            Assert.That(rules.offlineMaxSeconds, Is.EqualTo(86400));
            Assert.That(rules.offlineRateFactor, Is.EqualTo(0.5));
            Assert.That(rules.vitalityMaxBankSeconds, Is.EqualTo(3600));
            Assert.That(rules.vitalityOutputMultiplier, Is.EqualTo(1.05));
        }

        [Test]
        public void DuplicateStageAndUnknownInitialIdAreRejected()
        {
            var rules = new PrototypeRules();
            rules.stages[1].stageId = rules.stages[0].stageId;
            rules.initialStageId = "unknown";
            Assert.That(rules.Validate(), Is.Not.Empty);
            Assert.Throws<ArgumentException>(() => GameStateFactory.CreateNew(rules));
        }

        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidEconomyValueCannotInitialize(double value)
        {
            var rules = new PrototypeRules { rootBaseCost = value };
            Assert.That(rules.Validate(), Is.Not.Empty);
            Assert.Throws<ArgumentException>(() => GameStateFactory.CreateNew(rules));
        }

        [Test]
        public void ShrinkingCapAndCyclicStageChainAreRejected()
        {
            var rules = new PrototypeRules();
            rules.stages[1].rootCap = 1;
            rules.stages[2].nextStageId = "seedling";
            Assert.That(rules.Validate().Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void NewRunsHaveUniqueIdsAndIndependentMutableData()
        {
            var rules = new PrototypeRules();
            var a = GameStateFactory.CreateNew(rules);
            var b = GameStateFactory.CreateNew(rules);
            Assert.That(a.run.runId, Is.Not.EqualTo(b.run.runId));
            a.run.levels.root = 3;
            a.vitality.bankSeconds = 60;
            a.permanent.stagePoints[0].points = 2;
            Assert.That(b.run.levels.root, Is.Zero);
            Assert.That(b.vitality.bankSeconds, Is.Zero);
            Assert.That(b.permanent.stagePoints[0].points, Is.Zero);
            Assert.That(b.run.water, Is.EqualTo(10));
            Assert.That(b.run.hp, Is.EqualTo(100));
            Assert.That(b.run.stageId, Is.EqualTo("seedling"));
        }

        [Test]
        public void CreatedConfigHasAllValidPrefabViews()
        {
            var config = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ProjectSetup.ConfigPath);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Validate(), Is.Empty);
            foreach (var stage in config.Rules.stages)
                Assert.That(config.FindVisual(stage.stageId).prefab, Is.Not.Null);
        }

        [Test]
        public void SaveDtoCanRepresentVersionAndVitalityWithoutUnityReferences()
        {
            var state = GameStateFactory.CreateNew(new PrototypeRules());
            state.vitality.tapProgress = 49;
            state.vitality.bankSeconds = 3590;
            var dto = new SaveData { configVersion = "prototype-1", run = state.run, permanent = state.permanent, vitality = state.vitality, settings = state.settings };
            var restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(dto));
            Assert.That(restored.schemaVersion, Is.EqualTo(1));
            Assert.That(restored.vitality.tapProgress, Is.EqualTo(49));
            Assert.That(restored.vitality.bankSeconds, Is.EqualTo(3590));
            Assert.That(restored.permanent.stagePoints.Count, Is.EqualTo(4));
        }

        [Test]
        public void BuildEntryAndEditorSettingsPointToFoundationScene()
        {
            Assert.That(EditorBuildSettings.scenes.Length, Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(ProjectSetup.ScenePath));
            Assert.That(EditorBuildSettings.scenes[0].enabled, Is.True);
            Assert.That(EditorSettings.serializationMode, Is.EqualTo(SerializationMode.ForceText));
            Assert.That(EditorSettings.enterPlayModeOptionsEnabled, Is.False);
        }

        [Test]
        public void StagePrefabsUseImportedBlenderModelsUrpMaterialsAndTypedAnchors()
        {
            var config = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ProjectSetup.ConfigPath);
            Assert.That(config, Is.Not.Null);
            foreach (var stage in config.Rules.stages)
            {
                var visual = config.FindVisual(stage.stageId);
                Assert.That(visual, Is.Not.Null);
                var prefab = visual.prefab;
                Assert.That(prefab, Is.Not.Null);
                Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(prefab.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));

                var anchors = prefab.GetComponent<CayTheGioi.Presentation.TreeStageAnchors>();
                Assert.That(anchors, Is.Not.Null, "Missing TreeStageAnchors on " + stage.stageId);
                Assert.That(anchors.RootAnchor, Is.Not.Null);
                Assert.That(anchors.TrunkAuraAnchor, Is.Not.Null);
                Assert.That(anchors.LeavesCenterAnchor, Is.Not.Null);
                Assert.That(anchors.LightningTargetAnchor, Is.Not.Null);
                Assert.That(anchors.LightningTargetAnchor.localPosition, Is.Not.EqualTo(anchors.LeavesCenterAnchor.localPosition));
                Assert.That(anchors.HitColliders, Is.Not.Null);
                Assert.That(anchors.HitColliders.Length, Is.GreaterThanOrEqualTo(3));
                foreach (var hc in anchors.HitColliders)
                {
                    Assert.That(hc, Is.InstanceOf<MeshCollider>());
                    Assert.That(((MeshCollider)hc).convex, Is.False);
                }

                var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(3));
                foreach (var r in renderers)
                {
                    foreach (var mat in r.sharedMaterials)
                    {
                        Assert.That(mat, Is.Not.Null);
                        Assert.That(mat.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                    }
                }
            }

            var largeBark = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Large_Bark.mat");
            Assert.That(largeBark, Is.Not.Null);
            Assert.That(largeBark.GetTextureScale("_BaseMap"), Is.EqualTo(new Vector2(2.5f, 2.5f)));
            Assert.That(largeBark.GetColor("_BaseColor"), Is.EqualTo(Color.white));
        }
    }
}
