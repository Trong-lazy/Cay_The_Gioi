using System;
using System.Collections.Generic;
using CayTheGioi.Domain;
using UnityEngine;
using CayTheGioi.Presentation;

namespace CayTheGioi.Configuration
{
    [Serializable]
    public sealed class StageVisualDefinition
    {
        public string stageId;
        public GameObject prefab;
        public float cameraDistance = 6;
        public float cameraPitch = 25;
        public float cameraTargetHeight = 0.65f;
    }

    [Serializable]
    public sealed class TreeAnimationConfig
    {
        public float calmFlutterSpeed = 1.6f;
        public float thunderstormFlutterSpeed = 3.8f;
        public float seedlingFlutterAmpWorld = 0.0015f;
        public float youngTreeFlutterAmpWorld = 0.0025f;
        public float largeTreeFlutterAmpWorld = 0.0040f;
        public float thunderstormAmpMultiplier = 1.8f;
        public float seedlingMaxTiltAngle = 1.0f;
        public float youngTreeMaxTiltAngle = 2.0f;
        public float largeTreeMaxTiltAngle = 3.2f;
        public float tiltSmoothSpeed = 3.2f;

        public bool IsValid =>
            !float.IsNaN(calmFlutterSpeed) && calmFlutterSpeed > 0f &&
            !float.IsNaN(thunderstormFlutterSpeed) && thunderstormFlutterSpeed > 0f &&
            !float.IsNaN(seedlingFlutterAmpWorld) && seedlingFlutterAmpWorld >= 0f &&
            !float.IsNaN(youngTreeFlutterAmpWorld) && youngTreeFlutterAmpWorld >= 0f &&
            !float.IsNaN(largeTreeFlutterAmpWorld) && largeTreeFlutterAmpWorld >= 0f &&
            !float.IsNaN(thunderstormAmpMultiplier) && thunderstormAmpMultiplier > 0f &&
            !float.IsNaN(seedlingMaxTiltAngle) && seedlingMaxTiltAngle >= 0f &&
            !float.IsNaN(youngTreeMaxTiltAngle) && youngTreeMaxTiltAngle >= 0f &&
            !float.IsNaN(largeTreeMaxTiltAngle) && largeTreeMaxTiltAngle >= 0f &&
            !float.IsNaN(tiltSmoothSpeed) && tiltSmoothSpeed > 0f;
    }

    [Serializable]
    public sealed class WeatherVfxConfig
    {
        public float rainRate = 120f;
        public float rainStartSpeed = 14f;
        public float rainMinStreakSize = 0.015f;
        public float rainMaxStreakSize = 0.035f;
        public float skyLightningMinInterval = 4f;
        public float skyLightningMaxInterval = 9f;
        public float skyLightningDuration = 0.22f;
        public float skyLightningFlashIntensity = 1.55f;
        public float strikeFlashDuration = 0.35f;
        public float strikeFlashIntensity = 2.4f;

        public bool IsValid =>
            !float.IsNaN(rainRate) && rainRate > 0f &&
            !float.IsNaN(rainStartSpeed) && rainStartSpeed > 0f &&
            !float.IsNaN(rainMinStreakSize) && rainMinStreakSize > 0f &&
            !float.IsNaN(rainMaxStreakSize) && rainMaxStreakSize >= rainMinStreakSize &&
            !float.IsNaN(skyLightningMinInterval) && skyLightningMinInterval > 0f &&
            !float.IsNaN(skyLightningMaxInterval) && skyLightningMaxInterval >= skyLightningMinInterval &&
            !float.IsNaN(skyLightningDuration) && skyLightningDuration > 0f &&
            !float.IsNaN(skyLightningFlashIntensity) && skyLightningFlashIntensity >= 0f &&
            !float.IsNaN(strikeFlashDuration) && strikeFlashDuration > 0f &&
            !float.IsNaN(strikeFlashIntensity) && strikeFlashIntensity >= 0f;
    }

    [Serializable]
    public sealed class AudioConfig
    {
        public float defaultBgmVolume = 0.45f;
        public float defaultSfxVolume = 0.8f;
        public float calmRainVolume = 0.0f;
        public float rainyRainVolume = 0.24f;
        public float thunderstormRainVolume = 0.38f;
        public float calmWindVolume = 0.0f;
        public float sunnyWindVolume = 0.0f;
        public float rainyWindVolume = 0.08f;
        public float thunderstormWindVolume = 0.30f;

        public bool IsValid =>
            !float.IsNaN(defaultBgmVolume) && defaultBgmVolume >= 0f && defaultBgmVolume <= 1f &&
            !float.IsNaN(defaultSfxVolume) && defaultSfxVolume >= 0f && defaultSfxVolume <= 1f &&
            !float.IsNaN(calmRainVolume) && calmRainVolume >= 0f && calmRainVolume <= 1f &&
            !float.IsNaN(rainyRainVolume) && rainyRainVolume >= 0f && rainyRainVolume <= 1f &&
            !float.IsNaN(thunderstormRainVolume) && thunderstormRainVolume >= 0f && thunderstormRainVolume <= 1f &&
            !float.IsNaN(calmWindVolume) && calmWindVolume >= 0f && calmWindVolume <= 1f &&
            !float.IsNaN(sunnyWindVolume) && sunnyWindVolume >= 0f && sunnyWindVolume <= 1f &&
            !float.IsNaN(rainyWindVolume) && rainyWindVolume >= 0f && rainyWindVolume <= 1f &&
            !float.IsNaN(thunderstormWindVolume) && thunderstormWindVolume >= 0f && thunderstormWindVolume <= 1f;
    }

    [CreateAssetMenu(menuName = "Cây Thế Giới/Prototype Config")]
    public sealed class PrototypeConfig : ScriptableObject
    {
        [SerializeField] private PrototypeRules rules = new PrototypeRules();
        [SerializeField] private StageVisualDefinition[] visuals = new StageVisualDefinition[0];
        [SerializeField] private GameplayInputConfig input = new GameplayInputConfig();
        [SerializeField] private TreeAnimationConfig treeAnimation = new TreeAnimationConfig();
        [SerializeField] private WeatherVfxConfig weatherVfx = new WeatherVfxConfig();
        [SerializeField] private AudioConfig audioConfig = new AudioConfig();
        [SerializeField] private float evolutionTransitionSeconds = 0.8f;
        public float EvolutionTransitionSeconds => evolutionTransitionSeconds;
        public PrototypeRules Rules => rules;
        public GameplayInputConfig Input => input;
        public TreeAnimationConfig TreeAnimation => treeAnimation ?? (treeAnimation = new TreeAnimationConfig());
        public WeatherVfxConfig WeatherVfx => weatherVfx ?? (weatherVfx = new WeatherVfxConfig());
        public AudioConfig AudioConfig => audioConfig ?? (audioConfig = new AudioConfig());
        public StageVisualDefinition FindVisual(string stageId)
        {
            if (visuals != null)
                foreach (var visual in visuals)
                    if (visual != null && visual.stageId == stageId) return visual;
            return null;
        }

        public List<string> Validate()
        {
            var errors = rules == null ? new List<string> { "Rules are missing." } : rules.Validate();
            if (input == null || !input.IsValid) errors.Add("Input thresholds must be finite and positive.");
            if (treeAnimation != null && !treeAnimation.IsValid) errors.Add("Tree animation configuration parameters must be valid.");
            if (weatherVfx != null && !weatherVfx.IsValid) errors.Add("Weather VFX configuration parameters must be valid.");
            if (audioConfig != null && !audioConfig.IsValid) errors.Add("Audio configuration parameters must be valid.");
            if (float.IsNaN(evolutionTransitionSeconds) || float.IsInfinity(evolutionTransitionSeconds) || evolutionTransitionSeconds <= 0)
                errors.Add("Evolution transition seconds must be finite and positive.");
            var ids = new HashSet<string>();
            if (visuals == null) { errors.Add("Stage visuals are missing."); return errors; }
            foreach (var visual in visuals)
            {
                if (visual == null) { errors.Add("Visual entry is null."); continue; }
                if (string.IsNullOrWhiteSpace(visual.stageId) || !ids.Add(visual.stageId)) errors.Add("Visual stage IDs must be unique.");
                if (rules == null || rules.FindStage(visual.stageId) == null) errors.Add("Visual points to an unknown stage.");
                if (visual.prefab == null) errors.Add("Visual prefab is missing for " + visual.stageId);
                if (float.IsNaN(visual.cameraDistance) || float.IsInfinity(visual.cameraDistance) || visual.cameraDistance <= 0) errors.Add("Camera distance must be finite and positive.");
                if (float.IsNaN(visual.cameraPitch) || visual.cameraPitch < 10 || visual.cameraPitch > 70) errors.Add("Camera pitch must be within 10–70 degrees.");
                if (float.IsNaN(visual.cameraTargetHeight) || float.IsInfinity(visual.cameraTargetHeight) || visual.cameraTargetHeight < 0) errors.Add("Camera target height must be finite and nonnegative.");
            }
            if (rules != null && rules.stages != null)
                foreach (var stage in rules.stages)
                    if (stage != null && !ids.Contains(stage.stageId)) errors.Add("Missing visual for " + stage.stageId);
            return errors;
        }

#if UNITY_EDITOR
        public void ConfigureDefaults(StageVisualDefinition[] stageVisuals)
        {
            rules = new PrototypeRules();
            visuals = stageVisuals;
            treeAnimation = new TreeAnimationConfig();
            weatherVfx = new WeatherVfxConfig();
            audioConfig = new AudioConfig();
        }
#endif
    }
}
