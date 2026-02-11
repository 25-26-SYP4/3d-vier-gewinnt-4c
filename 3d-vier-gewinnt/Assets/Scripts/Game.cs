using UnityEngine;

public class Game : MonoBehaviour
{
    public Board board;
    public Player currentPlayer = Player.Player1;

    void Start()
    {
        board = new Board();
        Debug.Log("Spiel gestartet. Spieler 1 beginnt.");
    }

    public void TryMakeMove(int x, int y, int z)
    {
        Debug.Log("TryMakeMove aufgerufen");
        bool success = board.PlacePiece(x, y, z, currentPlayer);

        if (!success)
        {
            Debug.Log("Ungültiger Spielzug!");
            return;
        }

        Debug.Log($"Spielzug: {currentPlayer} -> {x},{y},{z}");

        if (board.CheckWin(currentPlayer))
        {
            Debug.Log(currentPlayer + " hat gewonnen!");
            enabled = false;
            return;
        }

        SwitchPlayer();
    }

    public void SwitchPlayer()
    {
        currentPlayer = currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;

        Debug.Log($"{currentPlayer} ist an der Reihe!");
    }

    
}
