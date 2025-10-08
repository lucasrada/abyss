using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonVisualizer : MonoBehaviour
{
    [Header("Decoración de Habitaciones")]
    public List<RoomDecorConfig> roomDecorConfigs;

    [System.Serializable]
    public class RoomDecorConfig
    {
        public string configName = "Habitación";
        public List<TileBase> floorDecorTiles;
        [Range(0, 1)]
        public float floorDecorChance = 0.08f;
        public List<TileBase> wallDecorTiles;
        [Range(0, 1)]
        public float wallDecorChance = 0.08f;
    }

    [Header("Tilemaps")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;

    [Header("Floor Tiles")]
    public TileBase floorTile;
    public TileBase startTile;

    [Header("Wall Generator")]
    public WallGenerator wallGenerator;

    // Decora habitaciones al final de pintar el mapa
    public void PaintRoomDecorations(Dungeon dungeon)
    {
        if (roomDecorConfigs == null || roomDecorConfigs.Count == 0)
        {
            return;
        }

        System.Random random = new System.Random();

        var rooms = dungeon.GetRooms();
        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            RoomDecorConfig decorConfig = roomDecorConfigs[Mathf.Min(i, roomDecorConfigs.Count - 1)];

            // Decoración de piso dentro de la habitación (no borde)
            for (int x = room.x + 1; x < room.x + room.width - 1; x++)
            {
                for (int y = room.y + 1; y < room.y + room.height - 1; y++)
                {
                    if (decorConfig.floorDecorTiles != null && decorConfig.floorDecorTiles.Count > 0 && random.NextDouble() < decorConfig.floorDecorChance)
                    {
                        TileBase decorTile = decorConfig.floorDecorTiles[random.Next(decorConfig.floorDecorTiles.Count)];
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), decorTile);
                    }
                }
            }

            // Decoración de paredes internas de la habitación
            // Bordes superior e inferior
            for (int x = room.x; x < room.x + room.width; x++)
            {
                foreach (int y in new int[] { room.y, room.y + room.height - 1 })
                {
                    if (decorConfig.wallDecorTiles != null && decorConfig.wallDecorTiles.Count > 0 && random.NextDouble() < decorConfig.wallDecorChance)
                    {
                        TileBase decorTile = decorConfig.wallDecorTiles[random.Next(decorConfig.wallDecorTiles.Count)];
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), decorTile);
                    }
                }
            }
            // Bordes izquierdo y derecho
            for (int y = room.y + 1; y < room.y + room.height - 1; y++)
            {
                foreach (int x in new int[] { room.x, room.x + room.width - 1 })
                {
                    if (decorConfig.wallDecorTiles != null && decorConfig.wallDecorTiles.Count > 0 && random.NextDouble() < decorConfig.wallDecorChance)
                    {
                        TileBase decorTile = decorConfig.wallDecorTiles[random.Next(decorConfig.wallDecorTiles.Count)];
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), decorTile);
                    }
                }
            }
        }
    }

    public void PaintFloorTiles(Dungeon dungeon, bool generateWalls = true)
    {
        char[,] dungeonLayout = dungeon.GetLayout();

        for (int x = 0; x < dungeonLayout.GetLength(0); x++)
        {
            for (int y = 0; y < dungeonLayout.GetLength(1); y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);

                if (dungeon.IsFloor(x, y))
                {
                    PaintTile(tilePosition, floorTilemap, floorTile);
                }
                else if (dungeon.IsStart(x, y))
                {
                    PaintTile(tilePosition, floorTilemap, startTile);
                }
            }
        }

        if (generateWalls)
        {
            wallGenerator.GenerateWalls(dungeon, this);
        }

        // Lógica de decoración de habitaciones (¡agregado!)
        PaintRoomDecorations(dungeon);
    }

    // Este método queda igual, no decora habitaciones porque no tiene acceso directo a ellas
    public void PaintFloorTiles(HashSet<Vector2Int> floorPositions, bool generateWalls = true)
    {
        foreach (var position in floorPositions)
        {
            PaintTile((Vector3Int)position, floorTilemap, floorTile);
        }

        if (generateWalls)
        {
            wallGenerator.GenerateWalls(floorPositions, this);
        }
        // NO llamamos a PaintRoomDecorations acá
    }

    private void PaintTile(Vector3Int position, Tilemap tilemap, TileBase tile)
    {
        tilemap.SetTile(position, tile);
    }

    public void Clear()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
    }

    public void PaintWallTile(Vector2Int position, TileBase wallTile)
    {
        PaintTile((Vector3Int)position, wallTilemap, wallTile);
    }
}
