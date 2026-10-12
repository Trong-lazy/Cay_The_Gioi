using System;
using CayTheGioi.Domain;
using UnityEngine;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class AudioController : MonoBehaviour
    {
        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private AudioSource rainSource;
        private AudioSource windSource;

        private AudioClip bgmClip;
        private AudioClip tapClip;
        private AudioClip upgradeClip;
        private AudioClip evolutionClip;
        private AudioClip achievementClip;
        private AudioClip shopClip;

        private AudioClip rainClip;
        private AudioClip windClip;
        private AudioClip distantThunderClip;
        private AudioClip strikeThunderClip;

        private float lastTapTime = -1f;
        public const float MinTapInterval = 0.08f; // 80ms rate limiting for tap SFX

        private bool musicEnabled = true;
        public bool MusicEnabled => musicEnabled;

        private WeatherSystem weather;
        private HealthStatusSystem health;
        private RebirthSystem rebirth;

        private float targetRainVolume;
        private float targetWindVolume;

        private CayTheGioi.Configuration.AudioConfig audioConfig = new CayTheGioi.Configuration.AudioConfig();

        public void Bind(GameState state, WeatherSystem weatherSystem = null, HealthStatusSystem healthSystem = null, RebirthSystem rebirthSystem = null, CayTheGioi.Configuration.AudioConfig config = null)
        {
            Unsubscribe();

            if (state != null && state.settings != null)
            {
                musicEnabled = state.settings.musicEnabled;
            }

            weather = weatherSystem;
            health = healthSystem;
            rebirth = rebirthSystem;
            if (config != null) audioConfig = config;

            EnsureAudioSources();
            EnsureClips();
            ApplyMusicSettings();

            if (weather != null)
            {
                weather.WeatherChanged += OnWeatherChanged;
                OnWeatherChanged(weather.CurrentWeatherId);
            }

            if (health != null)
            {
                health.RunDied += OnRunDied;
            }

            if (rebirth != null)
            {
                rebirth.Completed += OnRebirthCompleted;
            }
        }

        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                var bgmGo = new GameObject("BGM_Source", typeof(AudioSource));
                bgmGo.transform.SetParent(transform, false);
                bgmSource = bgmGo.GetComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = audioConfig.defaultBgmVolume;
            }

            if (sfxSource == null)
            {
                var sfxGo = new GameObject("SFX_Source", typeof(AudioSource));
                sfxGo.transform.SetParent(transform, false);
                sfxSource = sfxGo.GetComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = audioConfig.defaultSfxVolume;
            }

            if (rainSource == null)
            {
                var rainGo = new GameObject("Rain_Source", typeof(AudioSource));
                rainGo.transform.SetParent(transform, false);
                rainSource = rainGo.GetComponent<AudioSource>();
                rainSource.loop = true;
                rainSource.playOnAwake = false;
                rainSource.volume = 0f;
            }

            if (windSource == null)
            {
                var windGo = new GameObject("Wind_Source", typeof(AudioSource));
                windGo.transform.SetParent(transform, false);
                windSource = windGo.GetComponent<AudioSource>();
                windSource.loop = true;
                windSource.playOnAwake = false;
                windSource.volume = 0f;
            }
        }

        private void EnsureClips()
        {
            if (bgmClip == null) bgmClip = GenerateBgmClip();
            if (tapClip == null) tapClip = GenerateToneClip("SFX_Tap", 600f, 0.03f, 0.2f);
            if (upgradeClip == null) upgradeClip = GenerateChimeClip("SFX_Upgrade", 523.25f, 659.25f, 0.15f, 0.25f);
            if (evolutionClip == null) evolutionClip = GenerateChimeClip("SFX_Evolution", 440f, 880f, 0.4f, 0.3f);
            if (achievementClip == null) achievementClip = GenerateChimeClip("SFX_Achievement", 587.33f, 880f, 0.3f, 0.3f);
            if (shopClip == null) shopClip = GenerateChimeClip("SFX_Shop", 784f, 1046.5f, 0.18f, 0.25f);

            if (rainClip == null) rainClip = GenerateRainClip();
            if (windClip == null) windClip = GenerateWindClip();
            if (distantThunderClip == null) distantThunderClip = GenerateDistantThunderClip();
            if (strikeThunderClip == null) strikeThunderClip = GenerateStrikeThunderClip();

            if (bgmSource != null && bgmSource.clip == null) bgmSource.clip = bgmClip;
            if (rainSource != null && rainSource.clip == null) rainSource.clip = rainClip;
            if (windSource != null && windSource.clip == null) windSource.clip = windClip;
        }

        private bool isApplicationPaused;
        private bool isApplicationFocusLost;
        public bool IsSuspended => isApplicationPaused || isApplicationFocusLost;

        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;
            ApplyMusicSettings();
        }

        private void ApplyMusicSettings()
        {
            if (bgmSource != null)
            {
                bgmSource.mute = !musicEnabled;
                if (musicEnabled && !IsSuspended)
                {
                    if (!bgmSource.isPlaying && bgmSource.clip != null) bgmSource.Play();
                }
                else
                {
                    if (bgmSource.isPlaying) bgmSource.Pause();
                }
            }

            if (rainSource != null)
            {
                rainSource.mute = !musicEnabled;
                if (musicEnabled && !IsSuspended && targetRainVolume > 0.001f)
                {
                    if (!rainSource.isPlaying && rainSource.clip != null) rainSource.Play();
                }
                else if (IsSuspended || !musicEnabled)
                {
                    if (rainSource.isPlaying) rainSource.Pause();
                }
            }

            if (windSource != null)
            {
                windSource.mute = !musicEnabled;
                if (musicEnabled && !IsSuspended && targetWindVolume > 0.001f)
                {
                    if (!windSource.isPlaying && windSource.clip != null) windSource.Play();
                }
                else if (IsSuspended || !musicEnabled)
                {
                    if (windSource.isPlaying) windSource.Pause();
                }
            }

            if (sfxSource != null) sfxSource.mute = !musicEnabled;
        }

        private void OnWeatherChanged(string weatherId)
        {
            switch (weatherId)
            {
                case "rainy":
                    targetRainVolume = audioConfig.rainyRainVolume;
                    targetWindVolume = audioConfig.rainyWindVolume;
                    break;
                case "thunderstorm":
                    targetRainVolume = audioConfig.thunderstormRainVolume;
                    targetWindVolume = audioConfig.thunderstormWindVolume;
                    break;
                case "sunny":
                    targetRainVolume = 0.0f;
                    targetWindVolume = audioConfig.sunnyWindVolume;
                    break;
                case "calm":
                default:
                    targetRainVolume = audioConfig.calmRainVolume;
                    targetWindVolume = audioConfig.calmWindVolume;
                    break;
            }

            if (targetRainVolume > 0f && rainSource != null && !rainSource.isPlaying && !IsSuspended && musicEnabled)
            {
                rainSource.Play();
            }

            if (targetWindVolume > 0f && windSource != null && !windSource.isPlaying && !IsSuspended && musicEnabled)
            {
                windSource.Play();
            }
        }

        private void OnRunDied()
        {
            targetRainVolume = 0f;
            targetWindVolume = 0f;
            if (rainSource != null) { rainSource.Stop(); rainSource.volume = 0f; }
            if (windSource != null) { windSource.Stop(); windSource.volume = 0f; }
        }

        private void OnRebirthCompleted()
        {
            targetRainVolume = 0f;
            targetWindVolume = 0f;
            if (rainSource != null) { rainSource.Stop(); rainSource.volume = 0f; }
            if (windSource != null) { windSource.Stop(); windSource.volume = 0f; }
        }

        private void Update()
        {
            if (IsSuspended || !musicEnabled) return;

            float dt = Time.unscaledDeltaTime;

            // Ensure playback started if target volume is positive
            if (targetRainVolume > 0.001f && rainSource != null && !rainSource.isPlaying && rainSource.clip != null)
            {
                rainSource.Play();
            }
            if (targetWindVolume > 0.001f && windSource != null && !windSource.isPlaying && windSource.clip != null)
            {
                windSource.Play();
            }

            // Smooth crossfades for environmental audio
            if (rainSource != null)
            {
                rainSource.volume = Mathf.MoveTowards(rainSource.volume, targetRainVolume, dt * 0.35f);
                if (rainSource.volume <= 0.001f && targetRainVolume <= 0f && rainSource.isPlaying)
                {
                    rainSource.Pause();
                }
            }

            if (windSource != null)
            {
                windSource.volume = Mathf.MoveTowards(windSource.volume, targetWindVolume, dt * 0.35f);
                if (windSource.volume <= 0.001f && targetWindVolume <= 0f && windSource.isPlaying)
                {
                    windSource.Pause();
                }
            }
        }

        public void PlayDistantThunder()
        {
            if (!musicEnabled || IsSuspended) return;
            if (sfxSource != null && distantThunderClip != null)
            {
                sfxSource.PlayOneShot(distantThunderClip, 0.35f);
            }
        }

        public void PlayStrikeThunder()
        {
            if (!musicEnabled || IsSuspended) return;
            if (sfxSource != null && strikeThunderClip != null)
            {
                sfxSource.PlayOneShot(strikeThunderClip, 0.65f);
            }
        }

        public bool PlayTap()
        {
            float now = Time.unscaledTime;
            if (now - lastTapTime < MinTapInterval) return false;
            lastTapTime = now;

            if (sfxSource != null && tapClip != null && musicEnabled && !IsSuspended)
            {
                sfxSource.PlayOneShot(tapClip, 0.25f);
                return true;
            }
            return false;
        }

        public void PlayUpgrade()
        {
            if (sfxSource != null && upgradeClip != null && musicEnabled && !IsSuspended)
            {
                sfxSource.PlayOneShot(upgradeClip, 0.3f);
            }
        }

        public void PlayEvolution()
        {
            if (sfxSource != null && evolutionClip != null && musicEnabled && !IsSuspended)
            {
                sfxSource.PlayOneShot(evolutionClip, 0.4f);
            }
        }

        public void PlayAchievement()
        {
            if (sfxSource != null && achievementClip != null && musicEnabled && !IsSuspended)
            {
                sfxSource.PlayOneShot(achievementClip, 0.35f);
            }
        }

        public void PlayShop()
        {
            if (sfxSource != null && shopClip != null && musicEnabled && !IsSuspended)
            {
                sfxSource.PlayOneShot(shopClip, 0.3f);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            isApplicationPaused = pauseStatus;
            ApplyMusicSettings();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            isApplicationFocusLost = !hasFocus;
            ApplyMusicSettings();
        }

        // ── Procedural Audio Generation ─────────────────────────────────

        private static AudioClip GenerateToneClip(string name, float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.Max(1, (int)(sampleRate * duration));
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 25f);
                data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * volume * decay;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateChimeClip(string name, float freq1, float freq2, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.Max(1, (int)(sampleRate * duration));
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 8f);
                float f = Mathf.Lerp(freq1, freq2, t / duration);
                data[i] = (Mathf.Sin(2 * Mathf.PI * f * t) * 0.7f + Mathf.Sin(4 * Mathf.PI * f * t) * 0.3f) * volume * decay;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateBgmClip()
        {
            int sampleRate = 44100;
            float duration = 4.0f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float[] notes = { 261.63f, 329.63f, 392.00f, 523.25f };
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float wave = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float phase = 2 * Mathf.PI * notes[n] * t;
                    wave += Mathf.Sin(phase) * 0.25f;
                }
                float env = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * (t / duration));
                data[i] = wave * env * 0.12f;
            }

            var clip = AudioClip.Create("BGM_Procedural", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateRainClip()
        {
            int sampleRate = 44100;
            float duration = 3.0f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            // Filtered white noise with random droplet transients
            float lastVal = 0f;
            var rand = new System.Random(42);
            for (int i = 0; i < samples; i++)
            {
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                // Low-pass filter smoothing (simulating falling water)
                lastVal = Mathf.Lerp(lastVal, white, 0.18f);
                // Droplet transient spikes occasionally
                float droplet = (rand.NextDouble() < 0.003) ? (float)(rand.NextDouble() * 0.6) : 0f;
                data[i] = (lastVal * 0.75f + droplet) * 0.25f;
            }

            var clip = AudioClip.Create("SFX_Rain_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateWindClip()
        {
            int sampleRate = 44100;
            float duration = 3.5f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            // Swept low resonant howling wind loop
            float lastVal = 0f;
            var rand = new System.Random(108);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                // Resonant filter modulation
                float alpha = 0.04f + 0.02f * Mathf.Sin(2 * Mathf.PI * t / duration);
                lastVal = Mathf.Lerp(lastVal, white, alpha);
                float swell = 0.55f + 0.45f * Mathf.Sin(2 * Mathf.PI * t / duration);
                data[i] = lastVal * swell * 0.35f;
            }

            var clip = AudioClip.Create("SFX_Wind_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateDistantThunderClip()
        {
            int sampleRate = 44100;
            float duration = 2.2f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            var rand = new System.Random(256);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 1.8f);
                float rumble = Mathf.Sin(2 * Mathf.PI * 55f * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * 78f * t) * 0.4f;
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.3f;
                data[i] = (rumble + noise) * decay * 0.40f;
            }

            var clip = AudioClip.Create("SFX_Distant_Thunder", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateStrikeThunderClip()
        {
            int sampleRate = 44100;
            float duration = 2.5f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            var rand = new System.Random(512);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // High-frequency initial crack (0 - 0.15s)
                float crack = (t < 0.12f) ? ((float)(rand.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 28f) * 0.9f) : 0f;
                // Low resonant thunder body
                float bodyDecay = Mathf.Exp(-t * 2.0f);
                float lowBoom = Mathf.Sin(2 * Mathf.PI * 65f * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 95f * t) * 0.35f;
                float noiseTail = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.25f;
                data[i] = Mathf.Clamp(crack + (lowBoom + noiseTail) * bodyDecay * 0.55f, -1f, 1f);
            }

            var clip = AudioClip.Create("SFX_Strike_Thunder", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void Unsubscribe()
        {
            if (weather != null)
            {
                weather.WeatherChanged -= OnWeatherChanged;
            }

            if (health != null)
            {
                health.RunDied -= OnRunDied;
            }

            if (rebirth != null)
            {
                rebirth.Completed -= OnRebirthCompleted;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
