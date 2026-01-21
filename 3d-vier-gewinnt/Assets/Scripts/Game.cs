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
    
    
}
