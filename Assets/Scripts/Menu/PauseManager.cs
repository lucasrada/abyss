using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;   
    [SerializeField] private string menuSceneName = "Menu"; 

    private bool isPaused;

    void Awake()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        AudioListener.pause = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (pauseMenuUI) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        AudioListener.pause = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        AudioListener.pause = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(menuSceneName);
    }
}
