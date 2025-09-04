using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuButtons : MonoBehaviour
{
    [SerializeField] private MenuUIController ui;

    [SerializeField] private float clickDelay = 0.08f; // igual a tu Click Feedback o la duración del SFX

    public void NuevaPartida() => SceneManager.LoadScene("SampleScene");

    public void Opciones()
    {
        // deja que el sonido del botón suene y luego abre el panel
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
