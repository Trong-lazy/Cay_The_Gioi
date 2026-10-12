using System;
using System.IO;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class CosmeticTests
    {
        private PrototypeRules rules;
        private GameState state;
        private CosmeticSystem cosmetics;
        private string testDirectory;
        private SaveService saveService;
        private long revision;

        [SetUp]
        public void Setup()
        {
            rules = new PrototypeRules();
            state = GameStateFactory.CreateNew(rules);
            testDirectory = Path.Combine(Application.temporaryCachePath, "T08B_Edit_" + Guid.NewGuid().ToString("N"));
            saveService = new SaveService(rules, testDirectory);
            revision = 0;
            saveService.Save(state, "t08b", ref revision);

            cosmetics = new CosmeticSystem(state, rules, () =>
            {
                var r = saveService.Save(state, "t08b", ref revision);
                return new CommandResult(r.Success, r.Reason);
            });
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }

        [Test]
        public void Rules_ContainsDefaultCosmetic_GlowPlaceholder_WithPrice20()
        {
            Assert.That(rules.cosmetics, Is.Not.Null);
            Assert.That(rules.cosmetics.Length, Is.GreaterThanOrEqualTo(1));

            var glow = rules.FindCosmetic("glow_placeholder");
            Assert.That(glow, Is.Not.Null);
            Assert.That(glow.price, Is.EqualTo(20));
            Assert.That(string.IsNullOrWhiteSpace(glow.displayName), Is.False);
            Assert.That(rules.Validate(), Is.Empty);
        }

        [Test]
        public void CosmeticSystem_InitialState_NoneEquipped_EmptyOwned()
        {
            Assert.That(cosmetics.EquippedCosmeticId, Is.EqualTo("none"));
            Assert.That(cosmetics.IsOwned("glow_placeholder"), Is.False);
            Assert.That(cosmetics.IsEquipped("glow_placeholder"), Is.False);
        }

        [Test]
        public void CosmeticSystem_Buy_InsufficientCoin_Fails()
        {
            state.permanent.coin = 10; // Needs 20
            var res = cosmetics.TryBuy("glow_placeholder");
            Assert.That(res.Success, Is.False);
            Assert.That(res.Reason, Is.EqualTo("INSUFFICIENT_COIN"));
            Assert.That(state.permanent.coin, Is.EqualTo(10));
            Assert.That(cosmetics.IsOwned("glow_placeholder"), Is.False);
        }

        [Test]
        public void CosmeticSystem_Buy_SufficientCoin_DeductsCoin_AddsToOwned_DoesNotAutoEquip()
        {
            state.permanent.coin = 30;
            var res = cosmetics.TryBuy("glow_placeholder");
            Assert.That(res.Success, Is.True);
            Assert.That(state.permanent.coin, Is.EqualTo(10));
            Assert.That(cosmetics.IsOwned("glow_placeholder"), Is.True);
            // Must NOT auto equip on buy!
            Assert.That(cosmetics.EquippedCosmeticId, Is.EqualTo("none"));
            Assert.That(cosmetics.IsEquipped("glow_placeholder"), Is.False);
        }

        [Test]
        public void CosmeticSystem_Buy_DoublePurchase_RejectedAsAlreadyOwned()
        {
            state.permanent.coin = 50;
            var res1 = cosmetics.TryBuy("glow_placeholder");
            Assert.That(res1.Success, Is.True);
            Assert.That(state.permanent.coin, Is.EqualTo(30));

            // Second purchase attempt rejected
            var res2 = cosmetics.TryBuy("glow_placeholder");
            Assert.That(res2.Success, Is.False);
            Assert.That(res2.Reason, Is.EqualTo("ALREADY_OWNED"));
            Assert.That(state.permanent.coin, Is.EqualTo(30)); // Coin not deducted twice
        }

        [Test]
        public void CosmeticSystem_Buy_RollbackOnSaveFailure_DoesNotLoseCoinOrCorruptOwnership()
        {
            state.permanent.coin = 30;
            bool failSave = true;

            var sysWithFailingSave = new CosmeticSystem(state, rules, () =>
            {
                if (failSave) return new CommandResult(false, "DISK_ERROR");
                var r = saveService.Save(state, "t08b", ref revision);
                return new CommandResult(r.Success, r.Reason);
            });

            var resFail = sysWithFailingSave.TryBuy("glow_placeholder");
            Assert.That(resFail.Success, Is.False);
            Assert.That(resFail.Reason, Does.StartWith("SAVE_FAILED"));
            Assert.That(state.permanent.coin, Is.EqualTo(30)); // Coin restored
            Assert.That(sysWithFailingSave.IsOwned("glow_placeholder"), Is.False); // Ownership rolled back

            // Retry after save issue resolved
            failSave = false;
            var resSuccess = sysWithFailingSave.TryBuy("glow_placeholder");
            Assert.That(resSuccess.Success, Is.True);
            Assert.That(state.permanent.coin, Is.EqualTo(10));
            Assert.That(sysWithFailingSave.IsOwned("glow_placeholder"), Is.True);
        }

        [Test]
        public void CosmeticSystem_Equip_NotOwned_Fails()
        {
            var res = cosmetics.TryEquip("glow_placeholder");
            Assert.That(res.Success, Is.False);
            Assert.That(res.Reason, Is.EqualTo("NOT_OWNED"));
            Assert.That(cosmetics.EquippedCosmeticId, Is.EqualTo("none"));
        }

        [Test]
        public void CosmeticSystem_EquipAndUnequip_WhenOwned_UpdatesEquippedCosmeticId()
        {
            state.permanent.coin = 30;
            cosmetics.TryBuy("glow_placeholder");

            // Equip
            var equipRes = cosmetics.TryEquip("glow_placeholder");
            Assert.That(equipRes.Success, Is.True);
            Assert.That(cosmetics.EquippedCosmeticId, Is.EqualTo("glow_placeholder"));
            Assert.That(cosmetics.IsEquipped("glow_placeholder"), Is.True);

            // Re-equip is idempotent
            var reEquipRes = cosmetics.TryEquip("glow_placeholder");
            Assert.That(reEquipRes.Success, Is.True);
            Assert.That(cosmetics.EquippedCosmeticId, Is.EqualTo("glow_placeholder"));

            // Unequip
            var unequipRes = cosmetics.TryUnequip();
            Assert.That(unequipRes.Success, Is.True);
            Assert.That(cosmetics.EquippedCosmeticId, Is.EqualTo("none"));
            Assert.That(cosmetics.IsEquipped("glow_placeholder"), Is.False);
        }

        [Test]
        public void CosmeticSystem_Rebirth_PreservesOwnershipAndEquipped()
        {
            state.permanent.coin = 30;
            cosmetics.TryBuy("glow_placeholder");
            cosmetics.TryEquip("glow_placeholder");

            var rebirth = new RebirthSystem(state, rules, SaveSerializer.Clone, (c, id) =>
            {
                var r = saveService.Save(c, "t08b", ref revision);
                return new CommandResult(r.Success, r.Reason);
            });

            state.run.stageId = "young_tree";
            string runId = state.run.runId;
            var res = rebirth.TryRebirth(runId);
            Assert.That(res.Success, Is.True);

            // Ownership and equip preserved across Rebirth
            Assert.That(cosmetics.IsOwned("glow_placeholder"), Is.True);
            Assert.That(cosmetics.EquippedCosmeticId, Is.EqualTo("glow_placeholder"));
        }

        [Test]
        public void CosmeticSystem_SaveLoad_PreservesOwnershipAndEquipped()
        {
            state.permanent.coin = 50;
            cosmetics.TryBuy("glow_placeholder");
            cosmetics.TryEquip("glow_placeholder");

            Assert.That(saveService.Save(state, "t08b", ref revision).Success, Is.True);

            var loadRes = saveService.Load();
            Assert.That(loadRes.result.Success, Is.True);

            var loadedState = new GameState();
            SaveSerializer.Restore(loadRes.result.Data, loadedState);

            var loadedCosmetics = new CosmeticSystem(loadedState, rules);
            Assert.That(loadedCosmetics.IsOwned("glow_placeholder"), Is.True);
            Assert.That(loadedCosmetics.EquippedCosmeticId, Is.EqualTo("glow_placeholder"));
        }

        [Test]
        public void Cosmetic_DoesNotAffectGameplay_RatesHpProtection()
        {
            state.run.water = 100;
            state.run.growth = 50;
            state.run.hp = 100;
            state.run.levels.root = 2;
            state.run.levels.leaves = 1;
            state.run.levels.trunk = 1;

            var resources = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var beforeRates = resources.Rates();
            var upgrades = new UpgradeSystem(state, rules, resources);
            double beforeProtection = upgrades.Protection;

            // Equip cosmetic
            state.permanent.coin = 20;
            cosmetics.TryBuy("glow_placeholder");
            cosmetics.TryEquip("glow_placeholder");

            // Evaluate rates and protection after equip
            var afterRates = resources.Rates();
            double afterProtection = upgrades.Protection;

            Assert.That(afterRates.WaterProduced, Is.EqualTo(beforeRates.WaterProduced));
            Assert.That(afterRates.GrowthProduced, Is.EqualTo(beforeRates.GrowthProduced));
            Assert.That(afterProtection, Is.EqualTo(beforeProtection));
            Assert.That(state.run.hp, Is.EqualTo(100));
        }
    }
}
