using System.Drawing;
using UnityEngine;

public class Board
{
    public Player[,,] grid;
    public const int Size = 4;

    public Board()
    {
        grid = new Player[Size, Size, Size];

    }
    public void Clear()
    {
        for(int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for(int z = 0; z < Size; z++)
                {
                    grid[x, y, z] = Player.None;
                }
            }
        }
    }
    public bool IsFree(int x, int y, int z)
    {
        return grid[x, y, z] == Player.None;
    }
    public bool PlacePiece(int x, int y, int z, Player player)
    {
        if (!IsInside(x, y, z) || !IsFree(x, y, z)) return false;

        grid[x, y, z] = player;
        return true;
    }
    public Player GetCell(int x, int y, int z)
    {
        return grid[x, y, z];
    }
    private bool IsInside(int x, int y, int z)
    {
        return x >= 0 && x < Size &&
            y >= 0 && y < Size &&
            z >= 0 && z < Size;
    }
    public bool CheckWin(Player player)
    {
        int[,] directions = new int[,]
        {
            { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 },
            { 1, 1, 0 }, { 1, 0, 1 }, { 0, 1, 1 },
            { 1, 1, 1 }, { 1, -1, 1 }, { -1, 1, 1 }, { 1, 1, -1 }
        };

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int z = 0; z < Size; z++)
                {
                    if (grid[x, y, z] != player)
                    {
                        continue;
                    }

                    for (int d = 0;  d < directions.GetLength(0); d++)
                    {
                        
                    }
                }
            }
        }
        return true;
    }
    private bool HasFour(int x, int y, int z, int dx, int dy, int dz)
    {
        return true;
    }

}
