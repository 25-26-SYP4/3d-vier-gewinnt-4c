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
            gameManager.SwitchPlayer();
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

        MakeBarrel(newPiece, gameManager.currentPlayer == Player.Player1);

        spawnedPieces[new Vector3Int(x, y, z)] = newPiece;
        return newPiece;
    }

    private Sprite barrelP1, barrelP2;

    private Sprite LoadBarrel(ref Sprite cache, string path)
    {
        if (cache == null)
        {
            Texture2D t = Resources.Load<Texture2D>(path);
            if (t != null)
                cache = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return cache;
    }

    /// <summary>Ersetzt das 3D-Mesh des Steins durch ein Fass-Bild (Billboard).</summary>
    private void MakeBarrel(GameObject piece, bool player1)
    {
        // Player 1 = Giftmuell (gruen), Player 2 = Oel
        Sprite sprite = player1
            ? LoadBarrel(ref barrelP1, "PollutionTheme/icon_toxic")
            : LoadBarrel(ref barrelP2, "PollutionTheme/icon_oil");
        if (sprite == null) return;

        // Original 3D-Mesh ausblenden (Collider bleibt fuer Hoehe/Stacking)
        var mr = piece.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        float h = 1f;
        var col = piece.GetComponent<Collider>();
        if (col != null) h = col.bounds.size.y;
        if (h < 0.01f) h = 1f;

        var go = new GameObject("BarrelSprite");
        go.transform.SetParent(piece.transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // Volle Bildhoehe ~ 1.5x Steinhoehe -> Fass fuellt etwa die Steinhoehe,
        // damit gestapelte Fässer sich nur leicht beruehren statt zu ueberlappen.
        float full = sprite.bounds.size.y;
        float parentScaleY = Mathf.Max(0.001f, piece.transform.lossyScale.y);
        float scale = (h * 1.5f) / (Mathf.Max(0.001f, full) * parentScaleY);
        go.transform.localScale = Vector3.one * scale;

        // Leicht anheben, damit das Fass nicht im Boden/Board versinkt
        go.transform.localPosition = new Vector3(0f, h * 0.25f / parentScaleY, 0f);

        go.AddComponent<Billboard>();
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
    public void ShowMessage(string message)
    {
        messageText.text = message;
        messageGroup.alpha = 1f;

        CancelInvoke();
        Invoke(nameof(HideMessage), 2f);
    }

    void HideMessage()
    {
        messageGroup.alpha = 0f;
    }
}
