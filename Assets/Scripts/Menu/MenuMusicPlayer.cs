using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicPlayer : MonoBehaviour
{
    public AudioClip backgroundMusic;
    private AudioSource audioSource;
    private static MenuMusicPlayer instance;

    void Awake()
    {
        // Singleton para evitar duplicados
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (backgroundMusic != null && !audioSource.isPlaying)
        {
            audioSource.clip = backgroundMusic;
            audioSource.Play();
        }

        // Escuchá cambios de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si no estamos en la escena de menú, cortar música y destruir objeto
        if (scene.name != "MainMenu") // Cambia "MainMenu" por el nombre real de tu escena de menú
        {
            audioSource.Stop();
            Destroy(gameObject);
        }
    }
}
