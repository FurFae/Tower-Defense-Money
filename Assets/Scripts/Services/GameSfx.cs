using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    public static class GameSfx
    {
        public enum Event
        {
            ArrowShot, CannonShot, PoisonShot, IceShot, LightningShot, BurnShot, BallistaShot,
            Explosion, EnemyDeath, BossDeath, Build, Sell, Upgrade, WaveStart, Victory, Defeat
        }


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
            clip = Resources.Load<AudioClip>("BattleFoley/" + sfx);
            clips[sfx] = clip;
            return clip != null;
        }

    }
}
