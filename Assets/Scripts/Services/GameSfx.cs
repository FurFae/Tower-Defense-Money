using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    // Procedural battle sounds: no audio assets exist for shots/explosions yet, so the
    // clips are synthesized once at startup (tiny bursts of shaped noise/tone).
    // Replace with recorded samples later without touching the call sites.
    public static class GameSfx
    {
        public enum Event
        {
            ArrowShot, CannonShot, PoisonShot, IceShot, LightningShot, BurnShot, BallistaShot,
            Explosion, EnemyDeath, BossDeath, Build, Sell, Upgrade, WaveStart, Victory, Defeat
        }

        private const int SampleRate = 22050;
        private static readonly Dictionary<Event, AudioClip> clips = new Dictionary<Event, AudioClip>();
        private static AudioSource source;
        private static bool muted;

        public static bool Muted
        {
            get => muted;
            set { muted = value; if (source != null) source.mute = value; }
        }

        public static float Volume { get; set; } = 0.8f;

        private static AudioSource Source
        {
            get
            {
                if (source == null)
                {
                    GameObject go = new GameObject("GameSfx");
                    Object.DontDestroyOnLoad(go);
                    source = go.AddComponent<AudioSource>();
                    source.spatialBlend = 0f;
                    source.playOnAwake = false;
                    source.mute = muted;
                }
                return source;
            }
        }

        public static void Play(Event sfx, Vector3 worldPos = default)
        {
            if (!Ensure(sfx, out AudioClip clip)) return;
            // Positional feel without an AudioListener setup: quiet distant sounds.
            float volume = Volume;
            Camera cam = Camera.main;
            if (cam != null && worldPos != default)
            {
                float distance = Vector3.Distance(cam.transform.position, worldPos);
                volume *= Mathf.Clamp01(1.5f - distance * 0.08f);
            }
            Source.PlayOneShot(clip, volume);
        }

        private static bool Ensure(Event sfx, out AudioClip clip)
        {
            if (clips.TryGetValue(sfx, out clip) && clip != null) return true;
            clip = Synthesize(sfx);
            clips[sfx] = clip;
            return clip != null;
        }

        private static AudioClip Synthesize(Event sfx)
        {
            switch (sfx)
            {
                case Event.ArrowShot: return Tone(900f, 0.07f, 0.5f, 0.9f);
                case Event.BallistaShot: return Tone(320f, 0.15f, 0.8f, 0.7f);
                case Event.CannonShot: return NoiseBurst(0.35f, 0.9f, 0.6f);
                case Event.Explosion: return NoiseBurst(0.5f, 0.7f, 0.35f);
                case Event.PoisonShot: return Tone(520f, 0.12f, 0.4f, 0.6f);
                case Event.IceShot: return Tone(1400f, 0.18f, 0.35f, 0.4f);
                case Event.LightningShot: return NoiseBurst(0.12f, 0.8f, 0.2f);
                case Event.BurnShot: return NoiseBurst(0.25f, 0.5f, 0.5f);
                case Event.EnemyDeath: return Tone(180f, 0.2f, 0.6f, 0.5f);
                case Event.BossDeath: return NoiseBurst(0.9f, 0.8f, 0.3f);
                case Event.Build: return Tone(600f, 0.1f, 0.5f, 0.8f);
                case Event.Sell: return Tone(420f, 0.1f, 0.5f, 0.8f);
                case Event.Upgrade: return Chime(new[] { 520f, 660f, 780f }, 0.09f);
                case Event.WaveStart: return Chime(new[] { 330f, 440f }, 0.12f);
                case Event.Victory: return Chime(new[] { 523f, 659f, 784f, 1047f }, 0.14f);
                case Event.Defeat: return Chime(new[] { 392f, 311f, 233f }, 0.18f);
                default: return null;
            }
        }

        private static AudioClip Tone(float frequency, float duration, float attack, float decay)
        {
            int samples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Clamp01(t / (duration * attack)) * Mathf.Exp(-decay * t * 8f);
                // A sine plus a soft overtone reads better than a pure beep.
                data[i] = (Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.7f
                    + Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) * 0.3f) * envelope * 0.5f;
            }
            AudioClip clip = AudioClip.Create("sfx_tone", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip NoiseBurst(float duration, float attack, float decay)
        {
            int samples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[samples];
            Random.State state = Random.state;
            Random.InitState(12345);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Clamp01(t / (duration * attack)) * Mathf.Exp(-decay * t * 6f);
                data[i] = (Random.value * 2f - 1f) * envelope * 0.5f;
            }
            Random.state = state;
            AudioClip clip = AudioClip.Create("sfx_noise", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Chime(float[] notes, float noteDuration)
        {
            int perNote = Mathf.CeilToInt(SampleRate * noteDuration);
            float[] data = new float[perNote * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / SampleRate;
                    float envelope = Mathf.Exp(-t * 6f);
                    data[n * perNote + i] += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * envelope * 0.4f;
                }
            }
            AudioClip clip = AudioClip.Create("sfx_chime", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
