using UnityEngine;
using UnityEngine.Audio;

public class UISfxPlayer : MonoBehaviour
{
    public static UISfxPlayer Instance { get; private set; }

    [Header("Fuente de audio compartida (SIEMPRE activa)")]
    [SerializeField] private AudioSource sharedSource;

    private void Awake()
    {
        // Singleton sencillo
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!sharedSource)
            sharedSource = GetComponent<AudioSource>();

        if (!sharedSource)
            Debug.LogError("[UISfxPlayer] Falta AudioSource. Agregá uno y asignalo.");
        
        // Opcional si querés que siga viva entre escenas:
        // DontDestroyOnLoad(gameObject);
    }

    public void PlayOneShotSafe(AudioClip clip, float volume = 1f)
    {
        if (!clip || !sharedSource) return;
        // Aseguramos que la fuente no esté deshabilitada ni inactiva
        if (!sharedSource.enabled || !sharedSource.gameObject.activeInHierarchy) return;
        sharedSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
