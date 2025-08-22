using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtons : MonoBehaviour
{
    public void NuevaPartida()
    {
        Debug.Log("DEBUG: Entró a NuevaPartida()");
        SceneManager.LoadScene("SampleScene");
    }

    public void Salir()
    {
        Debug.Log("DEBUG: Botón Salir presionado");
        Application.Quit();
    }
}   
