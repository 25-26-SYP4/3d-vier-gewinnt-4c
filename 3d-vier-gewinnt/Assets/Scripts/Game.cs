using System;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
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

    public ClickSpawner clickSpawner;

    public SocketClient socket;

    private bool waitingForRobot = false;

    // Solange true, arbeitet der Roboter/Server noch am letzten Zug → kein neuer Zug.
    // Der ClickSpawner liest das, um Klicks während der Roboter-Arbeit zu sperren.
    public bool IsWaitingForRobot => waitingForRobot;

    public bool useBackend = true;

    void Start()
    {
        board = new Board();
        UpdatePlayerUI();

        if (useBackend)
        {
            socket = new SocketClient();

            try
            {
                socket.Connect();
            }
            catch
            {
                Debug.LogWarning("Backend nicht erreichbar.");
                useBackend = false;
            }
        }
    }

    public bool TryMakeMove(int x, int y, int z)
    {
        if (waitingForRobot)
        {
            Debug.Log("Warte auf Roboter...");
            return false;
        }
        if (gameOver) return false;

        Debug.Log("TryMakeMove aufgerufen");
        bool success = board.PlacePiece(x, y, z, currentPlayer);

        if (!success)
        {
            Debug.Log("Ungültiger Spielzug!");
            return false;
        }
        if (useBackend)
        {
            waitingForRobot = true;
            socket.Send(x, y, currentPlayer == Player.Player1 ? 1 : 2);
            StartCoroutine(WaitForRobot());
        }

        Debug.Log($"Spielzug: {currentPlayer} -> {x},{y},{z}");

        if (board.CheckWin(currentPlayer))
        {
            Debug.Log(currentPlayer + " hat gewonnen!");
            gameOver = true;
            HighlightWinningPieces();
            AnnounceWinner();
        }

        return true;
    }

    public void SwitchPlayer()
    {
        Debug.Log("SwitchPlayer aufgerufen");
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
            panel.color = new Color(55f / 255, 55f / 255, 55f / 255, 0.3f);
            text.color = Color.gray;
            panel.transform.localScale = Vector3.one;
        }
    }

    // Zeigt den Sieger als dauerhafte Meldung an (kein Popup mehr).
    void AnnounceWinner()
    {
        if (clickSpawner == null)
            clickSpawner = Object.FindFirstObjectByType<ClickSpawner>();

        if (clickSpawner != null)
            clickSpawner.ShowWinner(currentPlayer);
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
                    Renderer r = piece.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Renderer renderer = piece.GetComponent<Renderer>();
                        renderer.material.color = new Color(1f, 0.84f, 0f);
                    }
                }
            }
        }
    }

    IEnumerator WaitForRobot()
    {
        // socket.Receive() blockiert, bis der Server "DONE" schickt (~6 s).
        // Würde man es direkt in der Coroutine aufrufen, friert der komplette
        // Unity-Main-Thread ein. Deshalb läuft das Lesen in einem Hintergrund-Task,
        // und wir warten hier nicht-blockierend (yield return null) bis er fertig ist.
        var receiveTask = System.Threading.Tasks.Task.Run(() => socket.Receive());

        while (!receiveTask.IsCompleted)
            yield return null;

        if (receiveTask.IsFaulted)
        {
            Debug.LogError("Fehler beim Empfangen vom Server: " + receiveTask.Exception);
            waitingForRobot = false;
            yield break;
        }

        string response = receiveTask.Result;
        Debug.Log("Server Antwort: " + response);

        // Ab hier sind wir wieder im Main-Thread → SwitchPlayer/UI ist erlaubt.
        if (response == "DONE")
        {
            waitingForRobot = false;
            SwitchPlayer();
        }
    }
}
