using UnityEngine;

public class Mob : MonoBehaviour
{
    public int vidaBase = 10;
    public int dañoBase = 5;

    private int vidaActual;
    private int dañoActual;

    public void ConfigurarStats(int ronda)
    {
        vidaActual = vidaBase + (ronda * 5);
        dañoActual = dañoBase + (ronda * 2);
    }

    public void RecibirDaño(int daño)
    {
        vidaActual -= daño;
        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void Morir()
    {
        GameManager.Instance.EnemigoDerrotado();
        Destroy(gameObject);
    }
}
