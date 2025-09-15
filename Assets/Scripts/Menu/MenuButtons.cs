using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuButtons : MonoBehaviour
{
    [SerializeField] private MenuUIController ui;

    [SerializeField] private float clickDelay = 0.08f;

    public void NuevaPartida() => SceneManager.LoadScene("Level");

    public void Opciones()
    {
        StartCoroutine(OpenOptionsAfterClick());
    }

    private IEnumerator OpenOptionsAfterClick()
    {
        yield return new WaitForSecondsRealtime(clickDelay);
        if (ui) ui.ShowOptions();
        else Debug.LogError("[MenuButtons] Falta MenuUIController.");
    }

    public void Salir() => Application.Quit();
}
