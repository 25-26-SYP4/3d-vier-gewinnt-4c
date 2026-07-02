using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ClickSpawner : MonoBehaviour
{
    public GameObject GamePiecePlayer1;
    public GameObject GamePiecePlayer2;
    public Camera Camera;
    public float spawnHeightOffset = 2.5f;
    public PoleManager poleManager;

    private float pieceHeight;
    private Dictionary<Transform, int> poleStacks = new Dictionary<Transform, int>();

    public Game gameManager;
    
    public Dictionary<Vector3Int, GameObject> spawnedPieces = new Dictionary<Vector3Int, GameObject>();

    public CanvasGroup messageGroup;
    public TextMeshProUGUI messageText;

    private bool canPlace = true;

    // Dauerhafte Meldungen (bleiben stehen, bis sich der Zustand ändert):
    //   winnerMessage  → Sieger, höchste Priorität, bleibt bis zum Reset
    //   Roboter-Warten → solange gameManager.IsWaitingForRobot
    // Temporäre Meldungen (z. B. "Stab ist voll!") laufen weiter über ShowMessage.
    private string winnerMessage = null;
    private bool persistentActive = false;

    private void Awake()
    {
        spawnedPieces = new Dictionary<Vector3Int, GameObject>();
        poleStacks = new Dictionary<Transform, int>();
    }

    void Start()
    {
        pieceHeight = GamePiecePlayer1.GetComponent<Collider>().bounds.size.y;
    }

    void Update()
    {
        UpdateStatusMessage();

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Transform pole = GetPoleFromHit(hit.transform);

                if (pole != null)
                {
                    SpawnPieceOnPole(pole);
                }
            }
        }
    }

    void SpawnPieceOnPole(Transform poleTransform)
    {
        if (gameManager.gameOver) return;
        if (!canPlace) return;

        // Während der Roboter den vorigen Zug ausführt, keinen neuen Stein setzen.
        // Die Dauer-Meldung dazu zeigt UpdateStatusMessage automatisch an.
        if (gameManager.IsWaitingForRobot)
        {
            return;
        }

        Pole pole = poleTransform.GetComponent<Pole>();

        if (!poleStacks.ContainsKey(poleTransform))
            poleStacks[poleTransform] = 0;

        if(poleStacks[poleTransform] >= 4)
        {
            ShowMessage("Stab ist voll!");
            return;
        }

        Vector2Int index = poleManager.GetIndex(pole);

        int stackCount = poleStacks[poleTransform];

        int x = index.x;
        int y = index.y;
        int z = stackCount;

        Vector3 spawnPos = poleTransform.position;
        
        GameObject newPiece = SpawnGamePiece(spawnPos, stackCount, poleTransform, x, y, z);
        bool success = gameManager.TryMakeMove(x, y, z);

        if (!success && !gameManager.gameOver)
        {
            spawnedPieces.Remove(new Vector3Int(index.x, index.y, stackCount));
            Destroy(newPiece);
            return;
        }
        
        if (success || gameManager.gameOver)
        {
            // Spielerwechsel macht NUR der Game-Manager (nach "DONE" vom Roboter
            // bzw. sofort ohne Backend) – hier NICHT mehr, sonst doppelt.
            poleStacks[poleTransform]++;

            if (poleStacks[poleTransform] == 4)
            {
                poleTransform.GetComponent<Pole>().isFull = true;
            }

            canPlace = false;
            Invoke(nameof(ResetCooldown), 0.5f);
        }
    }
    
    private void ResetCooldown()
    {
        canPlace = true;
    }

    private GameObject SpawnGamePiece(Vector3 spawnPos, int stackCount, Transform poleTransform, int x, int y, int z)
    {
        spawnPos.y += spawnHeightOffset + stackCount * pieceHeight;
        
        GameObject newPiece;

        if (gameManager.currentPlayer == Player.Player1)
        {
            newPiece = Instantiate(GamePiecePlayer1, spawnPos, Quaternion.identity);
        }
        else
        {
            newPiece = Instantiate(GamePiecePlayer2, spawnPos, Quaternion.identity);            
        }

        newPiece.transform.SetParent(poleTransform);
        
        spawnedPieces[new Vector3Int(x, y, z)] = newPiece;
        return newPiece;
    }

    Transform GetPoleFromHit(Transform hit)
    {
        while (hit != null)
        {
            if (hit.CompareTag("Pole"))
                return hit;

            hit = hit.parent;
        }

        return null;
    }
    
    public void ResetPolesIsFull()
    {
        GameObject[] poles = GameObject.FindGameObjectsWithTag("Pole");

        foreach (GameObject poleObj in poles)
        {
            Pole pole = poleObj.GetComponent<Pole>();
            if (pole != null)
            {
                pole.isFull = false;
            }
        }
    }
    // Legt fest, welche DAUERHAFTE Meldung angezeigt wird. Priorität:
    //   1. Sieger  2. "Roboter führt Zug aus"  3. keine (dann dürfen temporäre
    //   Meldungen wie "Stab ist voll!" die Anzeige selbst steuern).
    private void UpdateStatusMessage()
    {
        string persistent = null;

        if (winnerMessage != null)
            persistent = winnerMessage;
        else if (gameManager != null && gameManager.IsWaitingForRobot)
            persistent = "Roboter führt den Zug aus … bitte warten";

        if (persistent != null)
        {
            // Dauer-Meldung erzwingen und ein evtl. laufendes Ausblenden stoppen.
            persistentActive = true;
            CancelInvoke(nameof(HideMessage));
            messageText.text = persistent;
            messageGroup.alpha = 1f;
        }
        else if (persistentActive)
        {
            // Gerade aus dem Dauer-Zustand herausgekommen → Meldung ausblenden.
            persistentActive = false;
            messageGroup.alpha = 0f;
        }
    }

    // Zeigt dauerhaft den Sieger an. Bleibt bis zum Reset stehen.
    public void ShowWinner(Player player)
    {
        winnerMessage = (player == Player.Player1 ? "Spieler 1" : "Spieler 2") + " hat gewonnen!";
    }

    // Temporäre Meldung (blendet nach 2 s wieder aus). Wird ignoriert, solange eine
    // Dauer-Meldung (Sieger / Roboter) aktiv ist, damit nichts überschrieben wird.
    public void ShowMessage(string message)
    {
        if (persistentActive) return;

        messageText.text = message;
        messageGroup.alpha = 1f;

        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 2f);
    }

    void HideMessage()
    {
        messageGroup.alpha = 0f;
    }
}
