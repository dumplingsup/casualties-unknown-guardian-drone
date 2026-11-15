using UnityEngine;

namespace GuardianDrone
{
    public static class DroneAudio
    {
        static AudioClip shot, heal, deploy, rotor;

        public static AudioClip Rotor => rotor ? rotor : rotor = Tone(78f, 0.4f, false, true);
        public static void Shot(Vector3 at) => Play(at, shot ? shot : shot = Tone(640f, 0.12f, true, false));
        public static void Heal(Vector3 at) => Play(at, heal ? heal : heal = Tone(420f, 0.18f, false, false));
        public static void Deploy(Vector3 at) => Play(at, deploy ? deploy : deploy = Tone(520f, 0.16f, false, false));

        static void Play(Vector3 at, AudioClip clip)
        {
            float volume = Plugin.Volume == null ? 0.35f : Plugin.Volume.Value;
            if (!clip || volume <= 0.001f) return;
            AudioSource.PlayClipAtPoint(clip, at, Mathf.Clamp01(volume));
        }

        static AudioClip Tone(float freq, float seconds, bool noise, bool loopable)
        {
            const int rate = 22050;
            int count = Mathf.Max(1, (int)(rate * seconds));
            var data = new float[count];
            var random = new System.Random(freq.GetHashCode());
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float edge = loopable ? 1f : Mathf.Clamp01(t / 0.012f) * Mathf.Clamp01((seconds - t) / 0.04f);
                float wave = noise ? (float)(random.NextDouble() * 2 - 1) : Mathf.Sin(t * freq * Mathf.PI * 2f);
                data[i] = wave * edge * 0.45f;
            }
            var clip = AudioClip.Create(noise ? "guardian-shot" : "guardian-tone", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
