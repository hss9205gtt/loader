using System.Collections.Generic;
using UnityEngine;

namespace NumberBlocks
{
    /// <summary>
    /// 효과음을 코드로 합성하고, 녹음한 목소리 파일이 있으면 그것을 우선 재생한다.
    /// 목소리 파일: Assets/Resources/Voices/1.wav ~ 10.wav ("하나", "둘" ...),
    /// great.wav ("잘했어요!") — 엄마 아빠 목소리로 녹음해 넣으면 된다.
    /// </summary>
    public class Sound : MonoBehaviour
    {
        const int Rate = 44100;

        // 도레미솔라 (펜타토닉) 위로 올라가는 음 — 숫자가 클수록 높은 소리
        static readonly float[] NumberNotes =
        {
            0f, 523.25f, 587.33f, 659.25f, 783.99f, 880.00f,
            1046.5f, 1174.7f, 1318.5f, 1568.0f, 1760.0f
        };

        AudioSource source;
        AudioClip pop, boing, whoosh, success;
        readonly Dictionary<int, AudioClip> tones = new Dictionary<int, AudioClip>();
        readonly Dictionary<string, AudioClip> voices = new Dictionary<string, AudioClip>();

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            pop = Synth("pop", 0.12f, t => Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(400, 900, t / 0.12f) * t), 0.5f);
            boing = Synth("boing", 0.35f, t => Mathf.Sin(2 * Mathf.PI * (220 + 60 * Mathf.Sin(t * 40)) * t), 0.5f);
            whoosh = Synth("whoosh", 0.18f, t => (Random.value * 2 - 1) * (1 - t / 0.18f), 0.25f);
            success = Synth("success", 0.8f, t =>
            {
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                int i = Mathf.Min(3, (int)(t / 0.15f));
                return Mathf.Sin(2 * Mathf.PI * notes[i] * t);
            }, 0.45f);
        }

        public void Pop() => source.PlayOneShot(pop);
        public void Boing() => source.PlayOneShot(boing);
        public void Whoosh() => source.PlayOneShot(whoosh);

        public void Success()
        {
            var v = Voice("great");
            source.PlayOneShot(v != null ? v : success);
        }

        /// <summary>숫자 n을 말한다 (녹음 파일이 없으면 n에 맞는 음을 낸다).</summary>
        public void Number(int n)
        {
            var v = Voice(n.ToString());
            if (v != null) { source.PlayOneShot(v); return; }
            if (!tones.TryGetValue(n, out var clip))
            {
                float f = NumberNotes[Mathf.Clamp(n, 1, NumberNotes.Length - 1)];
                clip = Synth("tone" + n, 0.25f, t => Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * f * t), 0.35f);
                tones[n] = clip;
            }
            source.PlayOneShot(clip);
        }

        AudioClip Voice(string key)
        {
            if (!voices.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>("Voices/" + key);
                voices[key] = clip;
            }
            return clip;
        }

        static AudioClip Synth(string name, float seconds, System.Func<float, float> wave, float volume)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                // 짧게 켜지고 서서히 꺼지는 envelope (딸깍 소리 방지)
                float env = Mathf.Min(1f, t / 0.01f) * Mathf.Pow(1f - t / seconds, 2f);
                data[i] = wave(t) * env * volume;
            }
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
