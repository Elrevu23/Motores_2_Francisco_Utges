using UnityEngine;

// Sonidos generados por código
public static class SoundFX
{
    private const int Rate = 44100;
    private static AudioClip step, loot, sell, alarm, ambience;

    public static void Step(Vector3 pos)
    {
        if (step == null) step = MakeClip("Paso", 0.15f, t => (Random.value * 2f - 1f) * Mathf.Exp(-t * 40f) * 0.35f);
        AudioSource.PlayClipAtPoint(step, pos, 0.6f);
    }

    public static void Loot()
    {
        if (loot == null) loot = MakeClip("Saqueo", 0.3f, t =>
            Mathf.Sin(2f * Mathf.PI * (t < 0.12f ? 880f : 1320f) * t) * Mathf.Exp(-t * 10f) * 0.4f);
        PlayUI(loot);
    }

    public static void Sell()
    {
        if (sell == null) sell = MakeClip("Venta", 0.5f, t =>
            (Mathf.Sin(2f * Mathf.PI * 1800f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 2400f * t)) * Mathf.Exp(-t * 12f) * 0.3f);
        PlayUI(sell);
    }

    // Alarma en bucle
    public static AudioSource StartAlarm(GameObject owner)
    {
        if (alarm == null) alarm = MakeClip("Alarma", 1f, t =>
            (Mathf.Sin(2f * Mathf.PI * 700f * t) >= 0f ? 1f : -1f) * (Mathf.Sin(2f * Mathf.PI * 3f * t) > 0f ? 0.15f : 0f));
        AudioSource src = owner.AddComponent<AudioSource>();
        src.clip = alarm;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0.6f;
        src.Play();
        return src;
    }

    // Ambiente de ciudad en bucle (archivo en Assets/Resources/Audio), muy bajo
    public static AudioSource StartAmbience(GameObject owner)
    {
        if (ambience == null) ambience = Resources.Load<AudioClip>("Audio/AmbienteCiudad");
        if (ambience == null) return null;
        AudioSource src = owner.AddComponent<AudioSource>();
        src.clip = ambience;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0.05f;
        src.Play();
        return src;
    }

    static void PlayUI(AudioClip clip)
    {
        Vector3 at = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, at, 0.8f);
    }

    static AudioClip MakeClip(string name, float seconds, System.Func<float, float> sample)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);

        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
