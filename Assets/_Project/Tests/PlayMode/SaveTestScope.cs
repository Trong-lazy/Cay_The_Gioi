using System;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace CayTheGioi.Tests
{
    internal static class SaveTestScope
    {
        private static string prior, directory;
        private static Func<long> priorClock;
        private static InputSettings.BackgroundBehavior priorBackgroundBehavior;
        private static InputSettings.EditorInputBehaviorInPlayMode priorEditorInputBehavior;
        private static bool inputPolicySaved;
        private static readonly List<InputDevice> nativePointers = new List<InputDevice>();
        public static void Begin()
        {
            nativePointers.Clear();
            // Physical pointers can overwrite queued virtual positions when Editor is focused.
            foreach(var device in InputSystem.devices)
                if(device.native && device.enabled && device is Pointer)nativePointers.Add(device);
            foreach(var device in nativePointers)InputSystem.DisableDevice(device);
            // Synthetic device events must reach uGUI even when Codex/Console has OS focus.
            // Restore both values in teardown; never persist this test-only policy.
            priorBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            priorEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            inputPolicySaved = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            prior = SaveService.ValidationDirectory;
            priorClock = SaveService.ValidationClock;
            SaveService.ValidationClock = () => 2000000000;
            directory = Path.Combine(Application.temporaryCachePath, "T04_PlayTests_" + Guid.NewGuid().ToString("N"));
            SaveService.ValidationDirectory = directory;
#if UNITY_EDITOR
            var type = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (type != null)
            {
                var win = UnityEditor.EditorWindow.GetWindow(type);
                if (win != null) win.Focus();
            }
#endif
        }
        public static void End()
        {
            foreach (var controller in UnityEngine.Object.FindObjectsByType<SaveController>()) controller.enabled = false;
            SaveService.ValidationDirectory = prior;
            SaveService.ValidationClock = priorClock;
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true);
            directory = null;
            if (inputPolicySaved)
            {
                InputSystem.settings.backgroundBehavior = priorBackgroundBehavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = priorEditorInputBehavior;
                inputPolicySaved = false;
            }
            foreach(var device in nativePointers)if(device.added)InputSystem.EnableDevice(device);
            nativePointers.Clear();
        }
    }
}
