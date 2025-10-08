using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class DeathScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject deathScreenCanvas;
    [Tooltip("First UI element to select when the death screen opens (optional).")]
    [SerializeField] private GameObject firstSelectedUI;

    // store previous state so we can restore it
    private bool prevCursorVisible;
    private CursorLockMode prevLockState;
    private float prevTimeScale;

    void Awake()
    {
        if (deathScreenCanvas != null) deathScreenCanvas.SetActive(false);

        // capture current cursor state
        prevCursorVisible = Cursor.visible;
        prevLockState = Cursor.lockState;
        prevTimeScale = Time.timeScale;
    }

    public void ShowDeathScreen()
    {
        if (deathScreenCanvas != null) deathScreenCanvas.SetActive(true);

        // Pause the game
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Force cursor visible & unlocked so the UI is interactable
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Select the Restart button (for keyboard/controller navigation)
        if (EventSystem.current != null && firstSelectedUI != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedUI);
        }
    }

    public void RestartLevel()
    {
        // restore time & cursor state
        Time.timeScale = prevTimeScale;
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevLockState;

        SceneManager.LoadScene("Level");
    }

    public void GoToMenu()
    {
        Time.timeScale = prevTimeScale;
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevLockState;

        SceneManager.LoadScene("Menu");
    }
}
