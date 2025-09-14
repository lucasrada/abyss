using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Exposed Parameter Names (Attenuation)")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string musicParam  = "MusicVolume";
    [SerializeField] private string sfxParam    = "SFXVolume";

    // --- Helpers: 0..1 lineal <-> dB ---
    public static float LinearToDb(float x)
    {
        return Mathf.Log10(Mathf.Clamp(x, 0.0001f, 1f)) * 20f; // ~-80..0 dB
    }

    public static float DbToLinear(float dB)
    {
        return Mathf.Pow(10f, dB / 20f);
    }

    // --- Setters ---
    public void SetMaster(float linear01)
    {
        if (!mixer) { Debug.LogError("[AudioManager] Mixer no asignado."); return; }
        mixer.SetFloat(masterParam, LinearToDb(linear01));
    }

    public void SetMusic(float linear01)
    {
        if (!mixer) { Debug.LogError("[AudioManager] Mixer no asignado."); return; }
        mixer.SetFloat(musicParam, LinearToDb(linear01));
    }

    public void SetSFX(float linear01)
    {
        if (!mixer) { Debug.LogError("[AudioManager] Mixer no asignado."); return; }
        mixer.SetFloat(sfxParam, LinearToDb(linear01));
    }

    // --- Getters ---
    public float GetMaster()
    {
        if (mixer && mixer.GetFloat(masterParam, out var dB)) return DbToLinear(dB);
        return 1f;
    }

    public float GetMusic()
    {
        if (mixer && mixer.GetFloat(musicParam, out var dB)) return DbToLinear(dB);
        return 1f;
    }

    public float GetSFX()
    {
        if (mixer && mixer.GetFloat(sfxParam, out var dB)) return DbToLinear(dB);
        return 1f;
    }
}
