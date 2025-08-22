using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int rondaActual = 1;
    public int enemigosRestantes;
    public int enemigosBase = 3;

    [Header("Referencias")]
    public GameObject portal;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        portal.SetActive(false);
        IniciarRonda();
    }

    public void IniciarRonda()
    {
        portal.SetActive(false);

        int cantidad = enemigosBase + (rondaActual * 2);
        enemigosRestantes = cantidad;

        EnemySpawner.Instance.SpawnEnemigos(cantidad, rondaActual);
    }

    public void EnemigoDerrotado()
    {
        enemigosRestantes--;

        if (enemigosRestantes <= 0)
        {
            portal.SetActive(true);
        }
    }

    public void SiguienteOleada()
    {
        rondaActual++;
        IniciarRonda();
    }
}
