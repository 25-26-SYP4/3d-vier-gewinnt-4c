using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public Board board;
    public Player currentPlayer = Player.Player1;
    public USBPIOController usbController;
    public Image player1Panel;
    public Image player2Panel;

    public TextMeshProUGUI player1Text;
    public TextMeshProUGUI player2Text;
    public bool gameOver = false;

    public GameObject endScreen;
    public TextMeshProUGUI playerWonText;

    void Start()
    {
        board = new Board();
        UpdatePlayerUI();
    }

    public void TryMakeMove(int x, int y, int z)
    {
        if (gameOver) return;

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
            board.Clear();
            
            enabled = false;
            gameOver = true;
            return;
        }

        SwitchPlayer();
    }

    public void SwitchPlayer()
    {
        currentPlayer = currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;

        UpdatePlayerUI();
    }
    public void UpdatePlayerUI()
    {
        if (currentPlayer == Player.Player1)
        {
            SetActivePlayer(player1Panel, player1Text, true);
            SetActivePlayer(player2Panel, player2Text, false);
        }
        else
        {
            SetActivePlayer(player1Panel, player1Text, false);
            SetActivePlayer(player2Panel, player2Text, true);
        }
    }
    void SetActivePlayer(Image panel, TextMeshProUGUI text, bool active)
    {
        if (active)
        {
            panel.color = new Color(55f / 255, 55f / 255, 55f / 255, 100f);
            text.color = Color.white;
            panel.transform.localScale = Vector3.one * 1.1f;
        }
        else
        {
            panel.color = new Color(55f/255, 55f/255, 55f / 255, 0.3f);
            text.color = Color.gray;
            panel.transform.localScale = Vector3.one;
        }
    }

    public void ShowEndScreen()
    {
        endScreen.SetActive(true);
        playerWonText.text = $"{(currentPlayer == Player.Player1 ? "Player 1" : "Player 2")} won";
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Object.FindFirstObjectByType<ClickSpawner>().ResetPolesIsFull();
        board = new Board();
    }
}
