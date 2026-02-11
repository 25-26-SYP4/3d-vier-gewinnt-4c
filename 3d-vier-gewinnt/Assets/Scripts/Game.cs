using UnityEngine;

public class Game : MonoBehaviour
{
    public Board board;
    public Player currentPlayer = Player.Player1;

    public bool gameOver = false;

    void Start()
    {
        board = new Board();
        Debug.Log("Spiel gestartet. Spieler 1 beginnt.");
    }

    public void TryMakeMove(int x, int y, int z)
    {
        if (gameOver) return;
        
        Debug.Log("TryMakeMove aufgerufen");
        bool success = board.PlacePiece(x, y, z, currentPlayer);

        if (!success)
        {
            Debug.Log("Ung�ltiger Spielzug!");
            return;
        }

        Debug.Log($"Spielzug: {currentPlayer} -> {x},{y},{z}");

        if (board.CheckWin(currentPlayer))
        {
            Debug.Log(currentPlayer + " hat gewonnen!");
            board.Clear();

            FindFirstObjectByType<ClickSpawner>().ResetBoardVisuals();
            enabled = false;
            gameOver = true;
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
