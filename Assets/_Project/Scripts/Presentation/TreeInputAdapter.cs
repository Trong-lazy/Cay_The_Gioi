using System.Collections.Generic;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    public sealed class TreeInputAdapter : MonoBehaviour
    {
        private VitalitySystem vitality;
        private GameState state;
        private GameplayInputConfig config;
        private TreeView tree;
        private OrbitCameraRig orbit;
        private readonly PointerGesture gesture = new PointerGesture();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private TouchControl owner;
        private Vector2 previous;
        private bool touchesBlocked, suppressMouse, rightDrag, focus = true, paused, startedOnTree;
        public bool GameplayAllowed { get; set; } = true;
        public bool CatchUpBlocked { get; set; }
        private bool Allowed => !CatchUpBlocked && GameplayAllowed && focus && !paused && state != null && state.run.life == "ALIVE";
        public void Bind(GameState game, VitalitySystem system, GameplayInputConfig settings, TreeView target, OrbitCameraRig rig)
        { state = game; vitality = system; config = settings; tree = target; orbit = rig; tree.ConfigureTapArea(config); Cancel(); }

        public bool IsOverUi(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            uiHits.Clear(); EventSystem.current.RaycastAll(pointer, uiHits);
            foreach (var hit in uiHits) if (hit.module is GraphicRaycaster) return true;
            return false;
        }
        public bool OnTree(Vector2 position)
        {
            if (orbit.SceneCamera == null || tree == null) return false;
            var ray = orbit.SceneCamera.ScreenPointToRay(position);

            bool prevBackfaces = Physics.queriesHitBackfaces;
            try
            {
                Physics.queriesHitBackfaces = true;
                var hits = Physics.RaycastAll(ray, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
                if (hits == null || hits.Length == 0) return false;
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    bool isTree = hit.collider.GetComponentInParent<TreeView>() == tree;
                    if (!isTree)
                    {
                        if (hit.collider.isTrigger) continue;
                        if (Vector3.Dot(hit.normal, ray.direction) < 0f) return false;
                        continue;
                    }

                    if (Vector3.Dot(hit.normal, ray.direction) < 0f) return true;

                    // Hit on tree is backface: check if there is a frontface on the same collider behind it
                    Ray nextRay = new Ray(hit.point + ray.direction * 0.0005f, ray.direction);
                    if (hit.collider.Raycast(nextRay, out RaycastHit nextHit, 50f))
                    {
                        if (Vector3.Dot(nextHit.normal, ray.direction) < 0f) return true;
                    }

                    // Otherwise it's a visible two-sided surface (such as SeedShell inner face)
                    return true;
                }
                return false;
            }
            finally
            {
                Physics.queriesHitBackfaces = prevBackfaces;
            }
        }
        private void Update()
        {
            if (vitality == null) return;
            if (!Allowed) { Cancel(); suppressMouse = true; touchesBlocked = true; return; }
            var screen = Touchscreen.current;
            int active = 0; bool touchFrame = false; TouchControl single = null;
            if (screen != null)
                foreach (var touch in screen.touches)
                {
                    if (touch.press.isPressed) { active++; single = touch; }
                    if (touch.press.isPressed || touch.press.wasReleasedThisFrame || touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled) touchFrame = true;
                }
            if (touchFrame)
            {
                suppressMouse = true; rightDrag = false;
                if (active > 1) { gesture.Cancel(); owner = null; touchesBlocked = true; }
                if (owner != null)
                {
                    var position = owner.position.ReadValue();
                    var phase = owner.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Canceled) { gesture.Cancel(); owner = null; }
                    else if (owner.press.wasReleasedThisFrame)
                    {
                        bool tap = gesture.Release(owner.touchId.ReadValue(), position, Time.unscaledTimeAsDouble, config.maximumTapSeconds, !Allowed || IsOverUi(position));
                        if (tap && startedOnTree && OnTree(position)) vitality.TryTap(Allowed);
                        owner = null;
                    }
                    else
                    {
                        gesture.Move(owner.touchId.ReadValue(), position);
                        if (IsOverUi(position)) { gesture.Cancel(); owner = null; touchesBlocked = true; }
                        else if (gesture.Dragged) orbit.Orbit(position - previous);
                        previous = position;
                    }
                }
                else if (active == 1 && !touchesBlocked && single.press.wasPressedThisFrame)
                {
                    gesture.Cancel(); owner = single; previous = single.position.ReadValue(); startedOnTree = OnTree(previous);
                    gesture.Begin(single.touchId.ReadValue(), previous, Time.unscaledTimeAsDouble, config.DragPixels(Screen.dpi, Screen.width, Screen.height), IsOverUi(previous));
                }
                if (active == 0) touchesBlocked = false;
                return;
            }
            if (owner != null) { gesture.Cancel(); owner = null; }
            touchesBlocked = false;
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (suppressMouse)
            {
                gesture.Cancel();
                if (!mouse.leftButton.isPressed && !mouse.rightButton.isPressed) suppressMouse = false;
                return;
            }
            var point = mouse.position.ReadValue();
            if (mouse.rightButton.wasPressedThisFrame)
            { gesture.Cancel(); rightDrag = !IsOverUi(point); }
            if (!mouse.rightButton.isPressed) rightDrag = false;
            if (rightDrag) { gesture.Cancel(); if (!IsOverUi(point)) orbit.Orbit(mouse.delta.ReadValue()); return; }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                startedOnTree = OnTree(point);
                gesture.Begin(mouse.deviceId, point, Time.unscaledTimeAsDouble, config.DragPixels(Screen.dpi, Screen.width, Screen.height), IsOverUi(point));
            }
            gesture.Move(mouse.deviceId, point);
            if (IsOverUi(point)) gesture.Cancel();
            if (mouse.leftButton.wasReleasedThisFrame && gesture.Release(mouse.deviceId, point, Time.unscaledTimeAsDouble, config.maximumTapSeconds, IsOverUi(point)) && startedOnTree && OnTree(point))
                vitality.TryTap(Allowed);
        }
        public void Cancel() { gesture.Cancel(); owner = null; rightDrag = false; }
        private void OnDisable() { Cancel(); }
        private void OnApplicationFocus(bool value) { focus = value; if (!value) { Cancel(); suppressMouse = true; touchesBlocked = true; } }
        private void OnApplicationPause(bool value) { paused = value; if (value) { Cancel(); suppressMouse = true; touchesBlocked = true; } }
    }
}
