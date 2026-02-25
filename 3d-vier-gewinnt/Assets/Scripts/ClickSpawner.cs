using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        Pole pole = poleTransform.GetComponent<Pole>();

        if (!poleStacks.ContainsKey(poleTransform))
            poleStacks[poleTransform] = 0;

        if(poleStacks[poleTransform] >= 4)
        {
            return;
        }

        Vector2Int index = poleManager.GetIndex(pole);

        int stackCount = poleStacks[poleTransform];

        int x = index.x;
        int y = index.y;
        int z = stackCount;

        Vector3 spawnPos = poleTransform.position;

        if (!gameManager.gameOver)
        {
            SpawnGamePiece(spawnPos, stackCount, poleTransform);
        }

        gameManager.TryMakeMove(x, y, z);

        poleStacks[poleTransform]++;

        if (poleStacks[poleTransform] == 4)
        {
            poleTransform.GetComponent<Pole>().isFull = true;
        }
    }

    private void SpawnGamePiece(Vector3 spawnPos, int stackCount, Transform poleTransform)
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
}
