using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [SerializeField] private GameObject enemigoPrefab;
    [SerializeField] private Tilemap floorTilemap;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnEnemigos(int cantidad, int ronda)
    {
        List<Vector3> posicionesValidas = ObtenerTilesDePiso();

        for (int i = 0; i < cantidad; i++)
        {
            if (posicionesValidas.Count == 0) return;

            int index = Random.Range(0, posicionesValidas.Count);
            Vector3 spawnPos = posicionesValidas[index];
            posicionesValidas.RemoveAt(index);

            GameObject enemigo = Instantiate(enemigoPrefab, spawnPos, Quaternion.identity);
            enemigo.GetComponent<Mob>().ConfigurarStats(ronda);
        }
    }

    private List<Vector3> ObtenerTilesDePiso()
    {
        List<Vector3> posiciones = new List<Vector3>();
        BoundsInt bounds = floorTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (floorTilemap.HasTile(pos))
                {
                    posiciones.Add(floorTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0));
                }
            }
        }

        return posiciones;
    }
}
