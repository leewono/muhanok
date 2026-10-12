using System;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation
{
    // All sound is synthesised at runtime, so no licensed audio files are needed.
    // Music: a step sequencer whose tempo/energy follows the interval phase. Siren: rises with chase pressure.
    public sealed class GameAudio : MonoBehaviour
    {
        private const int Rate = 44100;
        private RunSession session;
        private AudioSource music, siren, effects;
        private AudioClip coin, jump, hit, item, shield, beep, go;
        private float musicVolume = .35f, effectsVolume = .7f;
        // Read on the audio thread.
        private volatile float tempo = 96f, energy = .3f, level;
        private double phase;
        private int lastCoins, lastItems, lastShields;
        private bool wasAirborne;
        private GameFlowState lastState;
        public void Configure(RunSession run, float musicLevel, float effectsLevel)
        {
            session = run; musicVolume = musicLevel; effectsVolume = effectsLevel;
            music = Source("Music", true); music.clip = AudioClip.Create("Muhanok Music", Rate * 2, 1, Rate, true, FillMusic); music.loop = true; music.Play();
            siren = Source("Siren", true); siren.clip = Siren(); siren.loop = true; siren.volume = 0; siren.Play();
            effects = Source("Effects", false);
            coin = Tone(.12f, f => 1046 + (f > .05f ? 520 : 0), .5f);
            jump = Tone(.18f, f => 300 + f * 2600, .35f);
            item = Tone(.4f, f => 523 * Mathf.Pow(2, Mathf.Floor(f * 10) / 12f * 4), .4f);
            shield = Tone(.35f, f => 880 - f * 900, .45f);
            beep = Tone(.15f, f => 660, .4f);
            go = Tone(.35f, f => 990, .45f);
            hit = Noise(.4f);
        }
        private AudioSource Source(string id, bool loop)
        {
            var node = new GameObject(id); node.transform.SetParent(transform, false);
            var source = node.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = loop; source.spatialBlend = 0; return source;
        }
        public void SetVolumes(float musicLevel, float effectsLevel) { musicVolume = Mathf.Clamp01(musicLevel); effectsVolume = Mathf.Clamp01(effectsLevel); }
        public void Countdown(bool final) { if (effects != null) effects.PlayOneShot(final ? go : beep, effectsVolume); }

        private void Update()
        {
            if (session == null) return;
            var state = session.Flow.State;
            bool running = state == GameFlowState.Running && !session.Paused;
            // Sprint: fast and full; rest (cart): slow and soft; menus: calm.
            tempo = running ? (session.Resting ? 92f : 128f + 4f * Math.Min(6, session.IntervalCycle)) : 84f;
            energy = running ? (session.Resting ? .35f : 1f) : .3f;
            float target = state == GameFlowState.Result || state == GameFlowState.Caught ? .25f : running ? 1f : .6f;
            level = Mathf.MoveTowards(level, target, Time.unscaledDeltaTime);
            music.volume = musicVolume;
            float danger = running ? Mathf.Max(session.BananaPursuit ? 1f : 0f, Mathf.InverseLerp(.35f, .9f, session.Chase.Pressure)) : 0f;
            siren.volume = Mathf.MoveTowards(siren.volume, danger * effectsVolume * .45f, Time.unscaledDeltaTime);
            if (state == GameFlowState.Running)
            {
                if (session.Score.Coins > lastCoins) effects.PlayOneShot(coin, effectsVolume * .6f);
                if (session.Items.Collected > lastItems) effects.PlayOneShot(item, effectsVolume);
                if (session.ShieldSaves > lastShields) effects.PlayOneShot(shield, effectsVolume);
                bool airborne = session.Runner.Y > .05f;
                if (airborne && !wasAirborne) effects.PlayOneShot(jump, effectsVolume * .5f);
                wasAirborne = airborne;
            }
            if (state == GameFlowState.Caught && lastState != GameFlowState.Caught) effects.PlayOneShot(hit, effectsVolume);
            lastCoins = session.Score.Coins; lastItems = session.Items.Collected; lastShields = session.ShieldSaves; lastState = state;
        }

        // Audio thread: 16-step pattern in A minor pentatonic with kick, hat, bass and lead.
        private static readonly int[] Lead = { 0, 3, 5, 7, 10, 7, 5, 3, 0, 5, 7, 12, 10, 7, 5, 3 };
        private static readonly int[] Bass = { 0, 0, 7, 0, 5, 5, 3, 3 };
        private uint noise = 22222;
        private void FillMusic(float[] data)
        {
            float bpm = tempo, e = energy, l = level;
            double stepLength = Rate * 60.0 / bpm / 4.0;
            for (int i = 0; i < data.Length; i++)
            {
                int step = (int)(phase / stepLength) % 16;
                double inStep = (phase % stepLength) / stepLength;
                double t = phase / Rate;
                float kick = step % 4 == 0 ? (float)(Math.Sin(2 * Math.PI * (55 + 90 * Math.Exp(-inStep * 18)) * inStep * stepLength / Rate) * Math.Exp(-inStep * 7)) : 0f;
                noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                float hat = step % 2 == 1 ? ((noise & 0xFFFF) / 32768f - 1f) * (float)Math.Exp(-inStep * 30) * .25f * e : 0f;
                double bassFreq = 55 * Math.Pow(2, Bass[step / 2] / 12.0);
                float bass = (Math.Sin(2 * Math.PI * bassFreq * t) > 0 ? .22f : -.22f) * (float)Math.Exp(-inStep * 2.5);
                double leadFreq = 440 * Math.Pow(2, Lead[step] / 12.0);
                float lead = (float)(Math.Sin(2 * Math.PI * leadFreq * t) + .3 * Math.Sin(4 * Math.PI * leadFreq * t)) * (float)Math.Exp(-inStep * 5) * .12f * e;
                data[i] = (kick * .55f * (.4f + .6f * e) + hat + bass + lead) * .55f * l;
                phase += 1;
            }
            if (phase > Rate * 600.0) phase = 0;
        }

        private static AudioClip Tone(float seconds, Func<float, float> frequency, float volume)
        {
            int length = (int)(Rate * seconds); var samples = new float[length]; double phase = 0;
            for (int i = 0; i < length; i++)
            {
                float f = i / (float)length;
                phase += 2 * Math.PI * frequency(f) / Rate;
                samples[i] = (float)Math.Sin(phase) * volume * (1 - f) * Mathf.Clamp01(i / 200f);
            }
            var clip = AudioClip.Create("tone", length, 1, Rate, false); clip.SetData(samples, 0); return clip;
        }
        private static AudioClip Noise(float seconds)
        {
            int length = (int)(Rate * seconds); var samples = new float[length]; var random = new System.Random(7);
            for (int i = 0; i < length; i++) { float f = i / (float)length; samples[i] = ((float)random.NextDouble() * 2 - 1) * (1 - f) * (1 - f) * .7f + (float)Math.Sin(i * 2 * Math.PI * 70 / Rate) * (1 - f) * .5f; }
            var clip = AudioClip.Create("hit", length, 1, Rate, false); clip.SetData(samples, 0); return clip;
        }
        private static AudioClip Siren()
        {
            int length = Rate * 2; var samples = new float[length]; double phase = 0;
            for (int i = 0; i < length; i++)
            {
                double f = i < length / 2 ? 650 : 900;
                phase += 2 * Math.PI * f / Rate;
                samples[i] = (float)(Math.Sin(phase) > 0 ? .3 : -.3) * .5f + (float)Math.Sin(phase) * .25f;
            }
            var clip = AudioClip.Create("siren", length, 1, Rate, false); clip.SetData(samples, 0); return clip;
        }
    }
}
