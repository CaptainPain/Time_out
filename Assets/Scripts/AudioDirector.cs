using UnityEngine;

// All audio synthesized at runtime: engine hum pitched by speed, pendant
// sweep, landing thud, crash burst, win arpeggio. No audio files.
// Electric-guitar motifs per story beat (all synthesized, no audio files).
public enum Motif { None, Clean, Lonely, BluesRock, DarkDistorted }

public class AudioDirector : MonoBehaviour
{
    const int SR = 22050;

    static AudioSource engineSrc, sfxSrc, sirenSrc, motifSrc;
    static AudioClip engineClip, shiftClip, landClip, crashClip, winClip;
    static AudioClip deniedClip, sirenClip;
    static AudioClip cleanClip, lonelyClip, bluesClip, darkClip;
    static Motif currentMotif = Motif.None;

    void Awake()
    {
        engineSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc = gameObject.AddComponent<AudioSource>();
        sirenSrc = gameObject.AddComponent<AudioSource>();
        motifSrc = gameObject.AddComponent<AudioSource>();
        BuildAll();
        engineSrc.clip = engineClip;
        engineSrc.loop = true;
        engineSrc.volume = 0f;
        engineSrc.Play();
        sirenSrc.clip = sirenClip;
        sirenSrc.loop = true;
        sirenSrc.volume = 0.10f;
        motifSrc.loop = true;
        motifSrc.volume = 0f;
    }

    void OnDestroy()
    {
        engineSrc = null;
        sfxSrc = null;
        sirenSrc = null;
        motifSrc = null;
        currentMotif = Motif.None;
    }

    static void BuildAll()
    {
        // Destroyed clips compare == null, so a Play-mode re-entry rebuilds.
        if (engineClip == null) engineClip = EngineLoop();       // integer cycles: seamless loop
        if (shiftClip == null) shiftClip = Sweep(280f, 1500f, 0.45f);
        if (landClip == null) landClip = NoiseBurst(0.18f, 0.45f);
        if (crashClip == null) crashClip = NoiseBurst(0.5f, 0.8f);
        if (winClip == null) winClip = Arp();
        if (deniedClip == null) deniedClip = Buzz(130f, 0.22f, 0.40f);
        if (sirenClip == null) sirenClip = SirenLoop();
        if (cleanClip == null) cleanClip = GuitarMotif(new float[] { 329.63f, 392f, 493.88f, 392f }, 0.5f, 0.25f, false);
        if (lonelyClip == null) lonelyClip = GuitarMotif(new float[] { 220f, 0f, 174.61f, 0f }, 0.8f, 0.22f, false);
        if (bluesClip == null) bluesClip = GuitarMotif(new float[] { 146.83f, 174.61f, 196f, 174.61f, 146.83f, 130.81f }, 0.28f, 0.30f, false);
        if (darkClip == null) darkClip = GuitarMotif(new float[] { 110f, 116.54f, 110f, 103.83f }, 0.45f, 0.32f, true);
    }

    // Guitar motifs: plucked-string-ish notes (sine + harmonic, exponential
    // decay), looped. distort=true adds a harsh clipped edge for the pendant.
    static AudioClip GuitarMotif(float[] freqs, float noteDur, float vol, bool distort)
    {
        int per = (int)(SR * noteDur);
        var d = new float[per * freqs.Length];
        for (int k = 0; k < freqs.Length; k++)
        {
            float f = freqs[k];
            for (int i = 0; i < per; i++)
            {
                float t = i / (float)per;
                float s = 0f;
                if (f > 0f)
                {
                    s = Mathf.Sin(i * f / SR * Mathf.PI * 2f) * 0.6f
                      + Mathf.Sin(i * f * 2f / SR * Mathf.PI * 2f) * 0.25f
                      + Mathf.Sin(i * f * 3f / SR * Mathf.PI * 2f) * 0.12f;
                    if (distort) s = Mathf.Clamp(s * 2.2f, -0.9f, 0.9f);
                    s *= Mathf.Exp(-t * 4f);
                }
                d[k * per + i] = s * vol;
            }
        }
        return Make(d, "motif");
    }

    public static void PlayMotif(Motif m)
    {
        if (motifSrc == null || m == currentMotif) return;
        currentMotif = m;
        AudioClip c = null;
        switch (m)
        {
            case Motif.Clean: c = cleanClip; break;
            case Motif.Lonely: c = lonelyClip; break;
            case Motif.BluesRock: c = bluesClip; break;
            case Motif.DarkDistorted: c = darkClip; break;
        }
        if (c == null || m == Motif.None)
        {
            motifSrc.Stop();
            motifSrc.volume = 0f;
            return;
        }
        motifSrc.clip = c;
        motifSrc.volume = 0.35f;
        if (!motifSrc.isPlaying) motifSrc.Play();
    }

    public static void StopMotif()
    {
        PlayMotif(Motif.None);
    }

    public static void SetEngine(float speed01, bool throttle)
    {
        if (engineSrc == null) return;
        float wobble = 1f + 0.015f * Mathf.Sin(Time.time * 47f); // dirt-bike growl
        engineSrc.pitch = (0.65f + Mathf.Clamp01(speed01) * 1.5f) * wobble;
        float v = 0.10f + Mathf.Clamp01(speed01) * 0.08f + (throttle ? 0.06f : 0f);
        engineSrc.volume = Mathf.Lerp(engineSrc.volume, v, Time.deltaTime * 4f);
    }

    public static void PlayShift() { PlayOne(shiftClip); }
    public static void PlayLand() { PlayOne(landClip); }
    public static void PlayCrash() { PlayOne(crashClip); }
    public static void PlayWin() { PlayOne(winClip); }
    public static void PlayDenied() { PlayOne(deniedClip); }

    public static void SetSiren(bool on)
    {
        if (sirenSrc == null) return;
        if (on)
        {
            if (!sirenSrc.isPlaying) sirenSrc.Play();
        }
        else sirenSrc.Stop();
    }

    static void PlayOne(AudioClip c)
    {
        if (sfxSrc != null && c != null) sfxSrc.PlayOneShot(c);
    }

    static AudioClip Make(float[] d, string name)
    {
        var c = AudioClip.Create(name, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // Dirt-bike engine: 70 Hz saw + 35 Hz sub thump + 140 Hz chop.
    // All integer cycles in 1 s, so the loop is seamless.
    static AudioClip EngineLoop()
    {
        int n = SR;
        var d = new float[n];
        float ph1 = 0f, ph2 = 0f;
        for (int i = 0; i < n; i++)
        {
            ph1 += 70f / SR; if (ph1 >= 1f) ph1 -= 1f;
            ph2 += 35f / SR; if (ph2 >= 1f) ph2 -= 1f;
            float saw = (ph1 * 2f - 1f) * 0.32f;
            float sub = (ph2 * 2f - 1f) * 0.28f;
            float chop = 0.75f + 0.25f * Mathf.Sin(i * 140f / SR * Mathf.PI * 2f);
            d[i] = (saw + sub) * chop;
        }
        return Make(d, "engine");
    }

    static AudioClip Sweep(float f0, float f1, float dur)
    {
        int n = (int)(SR * dur);
        var d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float f = Mathf.Lerp(f0, f1, t);
            phase += f / SR;
            float env = Mathf.Sin(t * Mathf.PI); // smooth in/out
            d[i] = Mathf.Sin(phase * Mathf.PI * 2f) * env * 0.5f;
        }
        return Make(d, "sweep");
    }

    static AudioClip NoiseBurst(float dur, float vol)
    {
        int n = (int)(SR * dur);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            d[i] = (Random.value * 2f - 1f) * vol * Mathf.Exp(-t * 6f);
        }
        return Make(d, "noise");
    }

    static AudioClip Arp()
    {
        float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.5f };
        int per = (int)(SR * 0.13f);
        var d = new float[per * freqs.Length];
        for (int k = 0; k < freqs.Length; k++)
        {
            for (int i = 0; i < per; i++)
            {
                float t = i / (float)per;
                d[k * per + i] = Mathf.Sin(i * freqs[k] / SR * Mathf.PI * 2f) * (1f - t) * 0.4f;
            }
        }
        return Make(d, "arp");
    }

    static AudioClip TwoNote(float f0, float f1, float d0, float d1, float vol)
    {
        int n0 = (int)(SR * d0), n1 = (int)(SR * d1);
        var d = new float[n0 + n1];
        for (int i = 0; i < n0; i++)
        {
            float t = i / (float)n0;
            d[i] = Mathf.Sin(i * f0 / SR * Mathf.PI * 2f) * (1f - t) * vol;
        }
        for (int i = 0; i < n1; i++)
        {
            float t = i / (float)n1;
            d[n0 + i] = Mathf.Sin(i * f1 / SR * Mathf.PI * 2f) * (1f - t) * vol;
        }
        return Make(d, "twonote");
    }

    static AudioClip Buzz(float freq, float dur, float vol)
    {
        int n = (int)(SR * dur);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float s = Mathf.Sign(Mathf.Sin(i * freq / SR * Mathf.PI * 2f)) * 0.6f;
            d[i] = s * vol * Mathf.Exp(-t * 5f);
        }
        return Make(d, "buzz");
    }

    static AudioClip SirenLoop()
    {
        float dur = 2f;
        int n = (int)(SR * dur);
        var d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            int seg = (int)(t * 5f); // 5 x 0.4s: 700 950 700 950 700 (seamless loop)
            float f = (seg % 2 == 0) ? 700f : 950f;
            phase += f / SR;
            d[i] = Mathf.Sin(phase * Mathf.PI * 2f) * 0.35f;
        }
        return Make(d, "siren");
    }
}
