using UnityEngine;

public class PoleManager : MonoBehaviour
{
    public Pole[,] poles = new Pole[4, 4];

    public Pole[] row0 = new Pole[4];
    public Pole[] row1 = new Pole[4];
    public Pole[] row2 = new Pole[4];
    public Pole[] row3 = new Pole[4];

    void Awake()
    {
        for (int x = 0; x < 4; x++)
        {
            poles[x, 0] = row0[x];
            poles[x, 1] = row1[x];
            poles[x, 2] = row2[x];
            poles[x, 3] = row3[x];
        }
    }

    public Vector2Int GetIndex(Pole pole)
    {
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                if (poles[x, y] == pole)
                    return new Vector2Int(x, y);
            }
        }

        return new Vector2Int(-1, -1);
    }
}
