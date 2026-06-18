using System;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using System.Collections;

public class Game : MonoBehaviour
{
    public Board board;
    public Player currentPlayer = Player.Player1;
    public Image player1Panel;
    public Image player2Panel;

    public TextMeshProUGUI player1Text;
    public TextMeshProUGUI player2Text;
    public bool gameOver = false;

    public GameObject endScreen;
    public Button endscreenBackButton;
    public TextMeshProUGUI playerWonText;
    
    public ClickSpawner clickSpawner;

    public SocketClient socket;

    private bool waitingForRobot = false;

    void Start()
    {
        board = new Board();
        UpdatePlayerUI();
        socket = new SocketClient();
        socket.Connect();
    }

    public bool TryMakeMove(int x, int y, int z)
    {
        //if (waitingForRobot)
        //{
        //    Debug.Log("Warte auf Roboter...");
        //    return false;
        //}
        if (gameOver) return false;

        Debug.Log("TryMakeMove aufgerufen");
        bool success = board.PlacePiece(x, y, z, currentPlayer);

        if (!success)
        {
            Debug.Log("Ungültiger Spielzug!");
            return false;
        }
        waitingForRobot = true;
        socket.Send(x, y, currentPlayer == Player.Player1 ? 1 : 2);
        StartCoroutine(WaitForRobot());

        Debug.Log($"Spielzug: {currentPlayer} -> {x},{y},{z}");

        if (board.CheckWin(currentPlayer))
        {
            Debug.Log(currentPlayer + " hat gewonnen!");
            gameOver = true;
            HighlightWinningPieces();
            ShowEndScreen();
        }

        return true;
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
            // Volle Helligkeit, damit die Metallplatte/Emblem klar sichtbar ist
            panel.color = new Color(1f, 1f, 1f, 1f);
            text.color = Color.white;
            panel.transform.localScale = Vector3.one * 1.12f;
        }
        else
        {
            // Abgedunkelt + leicht transparent fuer den inaktiven Spieler
            panel.color = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            text.color = new Color(0.7f, 0.7f, 0.7f, 1f);
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

    public void HighlightWinningPieces()
    {
        clickSpawner = Object.FindFirstObjectByType<ClickSpawner>();

        if (clickSpawner == null) return;
        
        foreach (Vector3Int pos in board.winningPositions)
        {
            if (clickSpawner.spawnedPieces.TryGetValue(pos, out GameObject piece))
            {
                if (piece != null)
                {
                    // Fass-Sprite (Billboard) einfaerben
                    SpriteRenderer sr = piece.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(1f, 0.84f, 0f);
                    }
                    else
                    {
                        Renderer r = piece.GetComponent<Renderer>();
                        if (r != null)
                            r.material.color = new Color(1f, 0.84f, 0f);
                    }
                }
            }
        }
    }

    public void ViewBoard()
    {
        Debug.Log("ViewBoard aufgerufen");
        Debug.Log("Button vorher aktiv: " + endscreenBackButton.gameObject.activeSelf);
        endScreen.SetActive(false);
        endscreenBackButton.gameObject.SetActive(true);
        Debug.Log("Button nachher aktiv: " + endscreenBackButton.gameObject.activeSelf);
    }

    public void BackToEndScreen()
    {
        endScreen.SetActive(true);
        endscreenBackButton.gameObject.SetActive(false);
    }
    IEnumerator WaitForRobot()
    {
        string response = socket.Receive();

        Debug.Log("Server Antwort: " + response);

        if (response == "DONE")
        {
            waitingForRobot = false;
            // Hinweis: Spielerwechsel passiert in ClickSpawner (SpawnPieceOnPole),
            // damit es zuverlaessig abwechselt. Frueher wurde hier ZUSAETZLICH
            // SwitchPlayer() aufgerufen -> doppelter Wechsel -> blieb beim selben Spieler.
        }

        yield break;
    }
}
