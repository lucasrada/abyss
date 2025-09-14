using UnityEngine;

public class MenuUIController : MonoBehaviour
{
    [SerializeField] private GameObject panelMain;
    [SerializeField] private GameObject panelOptions;

    private void Awake()
    {
        ShowMain();
    }

    public void ShowMain()
    {
        if (panelMain)    panelMain.SetActive(true);
        if (panelOptions) panelOptions.SetActive(false);
    }

    public void ShowOptions()
    {
        if (panelMain)    panelMain.SetActive(false);
        if (panelOptions) panelOptions.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
