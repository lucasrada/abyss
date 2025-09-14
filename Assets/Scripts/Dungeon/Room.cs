using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Room
{
    public int x, y, width, height;
    private List<Room> connectedRooms = new List<Room>();

    public Room(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public bool Intersects(Room other)
    {
        return !(x + width <= other.x || 
                 other.x + other.width <= x || 
                 y + height <= other.y || 
                 other.y + other.height <= y);
    }

    public (int x, int y) Center()
    {
        return (x + width / 2, y + height / 2);
    }

    public void ConnectRoom(Room room)
    {
        if (!connectedRooms.Contains(room))
        {
            connectedRooms.Add(room);
        }
    }

    public List<Room> GetConnectedRooms()
    {
        return new List<Room>(connectedRooms);
    }

    public bool Contains(int checkX, int checkY)
    {
        return checkX >= x && checkX < x + width && checkY >= y && checkY < y + height;
    }

    public Vector2 GetRandomFloorPosition()
    {
        return new Vector2(
            Random.Range(x, x + width),
            Random.Range(y, y + height)
        );
    }

    public List<Vector2> GetAllFloorPositions()
    {
        List<Vector2> positions = new List<Vector2>();
        
        for (int floorX = x; floorX < x + width; floorX++)
        {
            for (int floorY = y; floorY < y + height; floorY++)
            {
                positions.Add(new Vector2(floorX, floorY));
            }
        }
        
        return positions;
    }

    public float GetArea()
    {
        return width * height;
    }

    public override string ToString()
    {
        return $"Room({x}, {y}, {width}x{height})";
    }
}