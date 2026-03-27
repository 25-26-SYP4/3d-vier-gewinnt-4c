using UnityEngine;

public class Board
{
    public Player[,,] grid;
    public const int Size = 4;

    public Vector3Int[] winningPositions = new Vector3Int[4];

    public Board()
    {
        grid = new Player[Size, Size, Size];
        Clear();
    }
    public void Clear()
    {
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int z = 0; z < Size; z++)
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
            { 1, 1, 0 }, { 1, -1, 0 },
            { 1, 0, 1 }, { 1, 0, -1 },
            { 0, 1, 1 }, { 0, 1, -1 },
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

                    for (int d = 0; d < directions.GetLength(0); d++)
                    {
                        if (HasFour(player, x, y, z,
                                directions[d, 0],
                                directions[d, 1],
                                directions[d, 2]))
                        {
                            Object.FindFirstObjectByType<Game>().ShowEndScreen();
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool HasFour(Player player, int x, int y, int z, int dx, int dy, int dz)
    {
        Vector3Int[] tempPositions = new Vector3Int[4];
        tempPositions[0] = new Vector3Int(x, y, z);

        int index = 1;

        index = CountDirection(player, x, y, z, dx, dy, dz, tempPositions, index);;
        index = CountDirection(player, x, y, z, -dx, -dy, -dz, tempPositions, index);

        if (index >= 4)
        {
            for (int i = 0; i < 4; i++)
            {
                winningPositions[i] = tempPositions[i];
            }

            return true;
        }

        return false;
    }

    private int CountDirection(Player player, int x, int y, int z, int dx, int dy, int dz, Vector3Int[] positions, int index)
    {
        for (int i = 1; i < Size; i++)
        {
            int nx = x + dx * i;
            int ny = y + dy * i;
            int nz = z + dz * i;

            if (nx < 0 || ny < 0 || nz < 0 ||
                nx >= Size || ny >= Size || nz >= Size)
                break;

            if (grid[nx, ny, nz] != player)
                break;

            if (index < 4)
            {
                positions[index] = new Vector3Int(nx, ny, nz);
                index++;
            }
        }
        return index;
    }

}
