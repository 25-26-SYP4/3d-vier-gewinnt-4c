using UnityEngine;

public class Game : MonoBehaviour
{
    private Board board;
    private Player currentPlayer = Player.Player1;

    void Start()
    {
        board = new Board();
        Debug.Log("Spiel gestartet. Spieler 1 beginnt.");
    }

    public void TryMakeMove(int x, int y, int z)
    {
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
        }

        SwitchPlayer();
    }

    private void SwitchPlayer()
    {
        currentPlayer = currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;

        Debug.Log($"{currentPlayer} ist an der Reihe!");
    }

    
}
