using System;
using System.Collections;
using CayTheGioi.Domain;
using UnityEngine;

namespace CayTheGioi.Presentation
{
    public sealed class WeatherView : MonoBehaviour
    {
        private WeatherSystem weather;
        private HealthStatusSystem health;
        private TreeView tree;
        private OrbitCameraRig cameraRig;
        private AudioController audioController;

        private Light sceneLight;
        private Color defaultLightColor = Color.white;
        private float defaultLightIntensity = 1f;

        private Color defaultAmbientColor = Color.gray;
        private bool defaultFogEnabled = false;
        private Color defaultFogColor = Color.gray;
        private float defaultFogDensity = 0.01f;
        private Camera sceneCamera;
        private Color defaultCameraBgColor = Color.blue;
        private bool envDefaultsCaptured = false;

        private ParticleSystem rainParticles;
        private LineRenderer strikeLightningRenderer;
        private LineRenderer skyLightningRenderer;

        private GameObject burningVisual;
        private Material burningFireMat;
        private Material burningSmokeMat;

        private Coroutine strikeFlashRoutine;
        private Coroutine skyLightningRoutine;
        private System.Random presentationRng = new System.Random(1337);

        private RebirthSystem rebirth;
        private CayTheGioi.Configuration.WeatherVfxConfig vfxConfig = new CayTheGioi.Configuration.WeatherVfxConfig();

        public void Bind(WeatherSystem weatherSystem, HealthStatusSystem healthSystem, TreeView treeView, OrbitCameraRig rig, AudioController audio = null, RebirthSystem rebirthSystem = null, CayTheGioi.Configuration.WeatherVfxConfig config = null)
        {
            Unsubscribe();
            weather = weatherSystem;
            health = healthSystem;
            tree = treeView;
            cameraRig = rig;
            audioController = audio != null ? audio : FindAnyObjectByType<AudioController>();
            rebirth = rebirthSystem;
            if (config != null) vfxConfig = config;

            sceneLight = Array.Find(FindObjectsByType<Light>(FindObjectsSortMode.None), light => light.type == LightType.Directional);
            if (sceneLight != null && sceneLight.type == LightType.Directional)
            {
                defaultLightColor = sceneLight.color;
                defaultLightIntensity = sceneLight.intensity;
            }

            sceneCamera = cameraRig != null ? cameraRig.GetComponentInChildren<Camera>() : Camera.main;
            if (sceneCamera == null) sceneCamera = FindAnyObjectByType<Camera>();

            if (!envDefaultsCaptured)
            {
                defaultAmbientColor = RenderSettings.ambientLight;
                defaultFogEnabled = RenderSettings.fog;
                defaultFogColor = RenderSettings.fogColor;
                defaultFogDensity = RenderSettings.fogDensity;
                if (sceneCamera != null) defaultCameraBgColor = sceneCamera.backgroundColor;
                envDefaultsCaptured = true;
            }

            SetupParticleAndLightning();

            if (weather != null)
            {
                weather.WeatherChanged += OnWeatherChanged;
                weather.LightningStruck += OnLightningStruck;
                weather.StatusChanged += UpdateBurningVisual;
            }

            if (health != null)
            {
                health.StatusChanged += UpdateBurningVisual;
                health.HealthChanged += UpdateBurningVisual;
            }

            if (rebirth != null)
            {
                rebirth.Completed += ResetForRebirth;
            }

            if (weather != null)
            {
                ApplyWeatherVisual(weather.CurrentWeatherId);
            }
            UpdateBurningVisual();
        }

        private void SetupParticleAndLightning()
        {
            // ── Rain Particle System ─────────────────────────────────────────
            if (rainParticles == null)
            {
                var rainGo = new GameObject("RainParticleSystem");
                rainGo.transform.SetParent(transform, false);
                rainParticles = rainGo.AddComponent<ParticleSystem>();

                var main = rainParticles.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startSpeed = new ParticleSystem.MinMaxCurve(vfxConfig.rainStartSpeed * 0.85f, vfxConfig.rainStartSpeed * 1.15f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.028f);
                main.startColor = new Color(0.82f, 0.90f, 1.0f, 0.45f);
                main.maxParticles = 1200;

                var shape = rainParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(5.5f, 5.5f, 1f);
                shape.position = Vector3.zero;
                shape.rotation = new Vector3(90f, 0f, 0f);

                var emission = rainParticles.emission;
                emission.rateOverTime = 0f;

                var rend = rainParticles.GetComponent<ParticleSystemRenderer>();
                rend.renderMode = ParticleSystemRenderMode.Stretch;
                rend.lengthScale = 1.0f;
                rend.velocityScale = 0.018f;
                rend.material = new Material(Shader.Find("Sprites/Default"));
            }

            // ── Damaging Lightning Bolt LineRenderer ─────────────────────────
            if (strikeLightningRenderer == null)
            {
                var strikeGo = new GameObject("LightningBolt");
                strikeGo.transform.SetParent(transform, false);
                strikeLightningRenderer = strikeGo.AddComponent<LineRenderer>();
                strikeLightningRenderer.positionCount = 4;
                strikeLightningRenderer.startWidth = 0.22f;
                strikeLightningRenderer.endWidth = 0.08f;
                strikeLightningRenderer.material = new Material(Shader.Find("Sprites/Default"));
                strikeLightningRenderer.startColor = Color.white;
                strikeLightningRenderer.endColor = new Color(0.85f, 0.92f, 1f, 0.9f);
                strikeLightningRenderer.enabled = false;
            }

            // ── Decorative Ambient Sky Lightning LineRenderer ────────────────
            if (skyLightningRenderer == null)
            {
                var skyGo = new GameObject("AmbientSkyLightningBolt");
                skyGo.transform.SetParent(transform, false);
                skyLightningRenderer = skyGo.AddComponent<LineRenderer>();
                skyLightningRenderer.positionCount = 4;
                skyLightningRenderer.startWidth = 0.12f;
                skyLightningRenderer.endWidth = 0.04f;
                skyLightningRenderer.material = new Material(Shader.Find("Sprites/Default"));
                skyLightningRenderer.startColor = new Color(0.9f, 0.95f, 1f, 0.85f);
                skyLightningRenderer.endColor = new Color(0.7f, 0.8f, 1f, 0.6f);
                skyLightningRenderer.enabled = false;
            }
        }

        private void OnWeatherChanged(string weatherId)
        {
            ApplyWeatherVisual(weatherId);
            UpdateBurningVisual();
        }

        public void GetBaseWeatherLight(string weatherId, out Color color, out float intensity)
        {
            switch (weatherId)
            {
                case "sunny":
                    color = new Color(1.0f, 0.97f, 0.88f);
                    intensity = defaultLightIntensity * 1.25f;
                    break;
                case "rainy":
                    color = new Color(0.65f, 0.75f, 0.88f);
                    intensity = defaultLightIntensity * 0.78f;
                    break;
                case "thunderstorm":
                    color = new Color(0.35f, 0.40f, 0.55f);
                    intensity = defaultLightIntensity * 0.45f;
                    break;
                case "calm":
                default:
                    color = defaultLightColor;
                    intensity = defaultLightIntensity;
                    break;
            }
        }

        private void ApplyWeatherVisual(string weatherId)
        {
            GetBaseWeatherLight(weatherId, out var targetColor, out var targetIntensity);
            if (sceneLight != null)
            {
                sceneLight.color = targetColor;
                sceneLight.intensity = targetIntensity;
            }

            if (strikeFlashRoutine != null)
            {
                StopCoroutine(strikeFlashRoutine);
                strikeFlashRoutine = null;
                if (strikeLightningRenderer != null) strikeLightningRenderer.enabled = false;
            }

            ApplyAtmosphere(weatherId);

            // Update Rain Particle Emission
            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                if (weatherId == "rainy")
                {
                    emission.rateOverTime = vfxConfig.rainRate;
                    if (!rainParticles.isPlaying) rainParticles.Play();
                }
                else if (weatherId == "thunderstorm")
                {
                    emission.rateOverTime = vfxConfig.rainRate * 2.5f;
                    if (!rainParticles.isPlaying) rainParticles.Play();
                }
                else
                {
                    emission.rateOverTime = 0f;
                }
            }

            // Manage Decorative Ambient Sky Lightning
            if (weatherId == "thunderstorm")
            {
                if (skyLightningRoutine == null)
                {
                    skyLightningRoutine = StartCoroutine(AmbientSkyLightningLoop());
                }
            }
            else
            {
                if (skyLightningRoutine != null)
                {
                    StopCoroutine(skyLightningRoutine);
                    skyLightningRoutine = null;
                }
                if (skyLightningRenderer != null) skyLightningRenderer.enabled = false;
            }
        }

        private void ApplyAtmosphere(string weatherId)
        {
            if (weatherId == "thunderstorm")
            {
                RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.22f);
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.15f, 0.18f, 0.26f);
                RenderSettings.fogDensity = 0.012f;
                if (sceneCamera != null) sceneCamera.backgroundColor = new Color(0.10f, 0.13f, 0.20f);
            }
            else if (weatherId == "rainy")
            {
                RenderSettings.ambientLight = new Color(0.24f, 0.28f, 0.36f);
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.28f, 0.32f, 0.40f);
                RenderSettings.fogDensity = 0.007f;
                if (sceneCamera != null) sceneCamera.backgroundColor = new Color(0.18f, 0.22f, 0.28f);
            }
            else
            {
                RestoreDefaultAtmosphere();
            }
        }

        private void RestoreDefaultAtmosphere()
        {
            if (envDefaultsCaptured)
            {
                RenderSettings.ambientLight = defaultAmbientColor;
                RenderSettings.fog = defaultFogEnabled;
                RenderSettings.fogColor = defaultFogColor;
                RenderSettings.fogDensity = defaultFogDensity;
                if (sceneCamera != null) sceneCamera.backgroundColor = defaultCameraBgColor;
            }
        }

        private void Update()
        {
            // Center rain box over tree/camera target so rain falls completely through tree to ground
            if (rainParticles != null)
            {
                Vector3 treePos = tree != null ? tree.transform.position : Vector3.zero;
                float treeTop = (tree != null && tree.LightningTargetAnchor != null)
                    ? tree.LightningTargetAnchor.position.y
                    : 1.0f;
                float emitterHeight = Mathf.Max(3.2f, treeTop + 2.5f);
                rainParticles.transform.position = new Vector3(treePos.x, emitterHeight, treePos.z);
                float minSpeed = Mathf.Max(1.0f, vfxConfig.rainStartSpeed * 0.85f);
                float fallDistance = emitterHeight + 1.2f;
                var main = rainParticles.main;
                main.startLifetime = fallDistance / minSpeed;
            }

            // Animate flame core if burning visual is active
            if (burningVisual != null && burningVisual.activeSelf)
            {
                AnimateBurningVisual();
            }
        }

        private IEnumerator AmbientSkyLightningLoop()
        {
            while (weather != null && weather.CurrentWeatherId == "thunderstorm")
            {
                // Configurable ambient interval
                float minWait = Mathf.Max(0.5f, vfxConfig.skyLightningMinInterval);
                float maxWait = Mathf.Max(minWait, vfxConfig.skyLightningMaxInterval);
                float waitTime = minWait + (float)presentationRng.NextDouble() * (maxWait - minWait);
                yield return new WaitForSeconds(waitTime);

                if (weather == null || weather.CurrentWeatherId != "thunderstorm") break;

                // Spawn sky bolt within camera upper viewport for guaranteed visibility
                Camera cam = Camera.main;
                if (cam == null) cam = FindAnyObjectByType<Camera>();
                if (cam != null)
                {
                    float u = 0.2f + (float)presentationRng.NextDouble() * 0.6f;
                    float v = 0.72f + (float)presentationRng.NextDouble() * 0.22f;
                    float dist = Mathf.Clamp(cam.nearClipPlane + 4.0f + (float)presentationRng.NextDouble() * 3.0f, 3.5f, 10.0f);
                    Vector3 p0 = cam.ViewportToWorldPoint(new Vector3(u, v, dist));
                    // Keep every point in the upper viewport at every stage's camera distance.
                    float endU = Mathf.Clamp(u + (float)presentationRng.NextDouble() * .24f - .12f, .15f, .85f);
                    float endV = Mathf.Max(.61f, v - .16f);
                    Vector3 p3 = cam.ViewportToWorldPoint(new Vector3(endU, endV, dist));
                    Vector3 p1 = cam.ViewportToWorldPoint(new Vector3(Mathf.Lerp(u, endU, .33f) + .025f, Mathf.Lerp(v, endV, .33f), dist));
                    Vector3 p2 = cam.ViewportToWorldPoint(new Vector3(Mathf.Lerp(u, endU, .66f) - .025f, Mathf.Lerp(v, endV, .66f), dist));

                    if (skyLightningRenderer != null)
                    {
                        skyLightningRenderer.SetPositions(new[] { p0, p1, p2, p3 });
                        skyLightningRenderer.enabled = true;
                    }
                }

                // Ambient light flash
                if (sceneLight != null)
                {
                    sceneLight.color = new Color(0.75f, 0.82f, 1.0f);
                    sceneLight.intensity = defaultLightIntensity * vfxConfig.skyLightningFlashIntensity;

                    if (audioController != null) audioController.PlayDistantThunder();

                    yield return new WaitForSeconds(Mathf.Max(0.04f, vfxConfig.skyLightningDuration * 0.5f));

                    GetBaseWeatherLight(weather != null ? weather.CurrentWeatherId : "calm", out var baseCol, out var baseInt);
                    sceneLight.color = baseCol;
                    sceneLight.intensity = baseInt;
                }

                yield return new WaitForSeconds(0.06f);
                if (skyLightningRenderer != null) skyLightningRenderer.enabled = false;
            }
            skyLightningRoutine = null;
        }

        private void OnLightningStruck(string regionId)
        {
            Transform targetAnchor = (tree != null && tree.LightningTargetAnchor != null)
                ? tree.LightningTargetAnchor
                : GetAnchor(regionId);

            Vector3 targetPos = targetAnchor != null ? targetAnchor.position : Vector3.up * 1.5f;
            Vector3 skyPos = targetPos + Vector3.up * 9.5f + new Vector3(
                ((float)presentationRng.NextDouble() - 0.5f) * 2.0f,
                0f,
                ((float)presentationRng.NextDouble() - 0.5f) * 2.0f
            );

            if (strikeFlashRoutine != null) StopCoroutine(strikeFlashRoutine);
            strikeFlashRoutine = StartCoroutine(DamagingStrikeEffect(skyPos, targetPos));

            // Trigger physical shudder reaction on tree
            if (tree != null)
            {
                var anim = tree.GetComponent<TreeAnimationController>();
                if (anim != null) anim.TriggerShudder(1.3f);
            }

            if (audioController != null)
            {
                audioController.PlayStrikeThunder();
            }
        }

        private IEnumerator DamagingStrikeEffect(Vector3 sky, Vector3 target)
        {
            if (strikeLightningRenderer != null)
            {
                Vector3 mid1 = Vector3.Lerp(sky, target, 0.33f) + UnityEngine.Random.insideUnitSphere * 0.7f;
                Vector3 mid2 = Vector3.Lerp(sky, target, 0.66f) + UnityEngine.Random.insideUnitSphere * 0.4f;
                strikeLightningRenderer.positionCount = 4;
                strikeLightningRenderer.SetPositions(new[] { sky, mid1, mid2, target });
                strikeLightningRenderer.enabled = true;
            }

            if (sceneLight != null)
            {
                sceneLight.color = Color.white;
                sceneLight.intensity = vfxConfig.strikeFlashIntensity;

                yield return new WaitForSeconds(Mathf.Max(0.05f, vfxConfig.strikeFlashDuration * 0.4f));

                GetBaseWeatherLight(weather != null ? weather.CurrentWeatherId : "calm", out var baseCol, out var baseInt);
                sceneLight.color = baseCol;
                sceneLight.intensity = baseInt;
            }

            yield return new WaitForSeconds(0.12f);
            if (strikeLightningRenderer != null) strikeLightningRenderer.enabled = false;
        }

        public void UpdateBurningVisual()
        {
            bool burning = health != null && health.IsBurning;
            if (!burning)
            {
                if (burningVisual != null) burningVisual.SetActive(false);
                return;
            }

            Transform anchor = GetAnchor(health.ImpactRegionId);
            if (anchor == null)
            {
                if (burningVisual != null) burningVisual.SetActive(false);
                return;
            }

            if (burningVisual == null)
            {
                SetupBurningVisualHierarchy();
            }

            burningVisual.transform.SetParent(anchor, false);
            burningVisual.transform.localPosition = Vector3.zero;
            burningVisual.SetActive(true);
        }

        private void SetupBurningVisualHierarchy()
        {
            burningVisual = new GameObject("BurningIndicator");

            // Inner flame sphere
            var flameCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flameCore.name = "FlameCore";
            DestroyImmediate(flameCore.GetComponent<Collider>());
            flameCore.transform.SetParent(burningVisual.transform, false);
            flameCore.transform.localScale = Vector3.one * 0.42f;

            var flameRend = flameCore.GetComponent<Renderer>();
            burningFireMat = new Material(Shader.Find("Sprites/Default"));
            burningFireMat.color = new Color(1.0f, 0.42f, 0.05f, 0.90f);
            flameRend.material = burningFireMat;

            // Outer flickering halo
            var flameHalo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flameHalo.name = "FlameHalo";
            DestroyImmediate(flameHalo.GetComponent<Collider>());
            flameHalo.transform.SetParent(burningVisual.transform, false);
            flameHalo.transform.localScale = Vector3.one * 0.65f;
            flameHalo.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            var haloRend = flameHalo.GetComponent<Renderer>();
            burningSmokeMat = new Material(Shader.Find("Sprites/Default"));
            burningSmokeMat.color = new Color(0.95f, 0.20f, 0.02f, 0.50f);
            haloRend.material = burningSmokeMat;
        }

        private void AnimateBurningVisual()
        {
            float t = Time.time * 9.0f;
            float pulse = 0.90f + Mathf.Sin(t) * 0.18f + Mathf.Cos(t * 1.7f) * 0.10f;
            if (burningVisual != null)
            {
                var core = burningVisual.transform.Find("FlameCore");
                if (core != null)
                {
                    core.localScale = Vector3.one * (0.38f * pulse);
                }
                var halo = burningVisual.transform.Find("FlameHalo");
                if (halo != null)
                {
                    halo.localScale = Vector3.one * (0.60f * (1.1f - pulse * 0.2f));
                    halo.localPosition = new Vector3(
                        Mathf.Sin(t * 1.3f) * 0.05f,
                        0.14f + Mathf.Cos(t * 0.8f) * 0.04f,
                        Mathf.Cos(t * 1.1f) * 0.05f
                    );
                }
            }
        }

        private Transform GetAnchor(string regionId)
        {
            if (tree == null) return null;
            switch (regionId)
            {
                case "root": return tree.RootAnchor;
                case "leaves": return tree.LeavesAnchor;
                case "trunk": return tree.TrunkAnchor;
                default: return tree.LeavesAnchor ?? tree.transform;
            }
        }

        public void ResetForRebirth()
        {
            if (skyLightningRoutine != null) { StopCoroutine(skyLightningRoutine); skyLightningRoutine = null; }
            if (strikeFlashRoutine != null) { StopCoroutine(strikeFlashRoutine); strikeFlashRoutine = null; }
            if (skyLightningRenderer != null) skyLightningRenderer.enabled = false;
            if (strikeLightningRenderer != null) strikeLightningRenderer.enabled = false;
            if (burningVisual != null) burningVisual.SetActive(false);
            ApplyWeatherVisual("calm");
        }

        private void Unsubscribe()
        {
            if (weather != null)
            {
                weather.WeatherChanged -= OnWeatherChanged;
                weather.LightningStruck -= OnLightningStruck;
                weather.StatusChanged -= UpdateBurningVisual;
            }
            if (health != null)
            {
                health.StatusChanged -= UpdateBurningVisual;
                health.HealthChanged -= UpdateBurningVisual;
            }
            if (rebirth != null)
            {
                rebirth.Completed -= ResetForRebirth;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (skyLightningRoutine != null) StopCoroutine(skyLightningRoutine);
            if (strikeFlashRoutine != null) StopCoroutine(strikeFlashRoutine);
            if (sceneLight != null)
            {
                sceneLight.color = defaultLightColor;
                sceneLight.intensity = defaultLightIntensity;
            }
            RestoreDefaultAtmosphere();
        }
    }
}
