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

        Vector2Int index = poleManager.GetIndex(pole);

        int stackCount = poleStacks[poleTransform];

        int x = index.x;
        int y = index.y;
        int z = stackCount;

        Vector3 spawnPos = poleTransform.position;

        // Game - TryMakeMove(x, y, z) aufrufen
        gameManager.TryMakeMove(x, y, z);

        spawnPos.y += spawnHeightOffset + stackCount * pieceHeight;

        if (gameManager.currentPlayer == Player.Player1)
        {
            Instantiate(GamePiecePlayer1, spawnPos, Quaternion.identity);
        }
        else
        {
            Instantiate(GamePiecePlayer2, spawnPos, Quaternion.identity);
        }

        poleStacks[poleTransform]++;

        if (poleStacks[poleTransform] == 4)
        {
            poleTransform.GetComponent<Collider>().enabled = false;
            return;
        }
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
    public void ResetBoardVisuals()
    {
        StartCoroutine(ResetRoutine(5));
    }
    IEnumerator ResetRoutine(float delay)
    {
        Debug.Log("Gewinn! Brett wird gleich zurückgesetzt...");

        yield return new WaitForSeconds(delay);

        GameObject[] pieces = GameObject.FindGameObjectsWithTag("GamePiece");

        foreach (GameObject go in pieces)
            Destroy(go);

        poleStacks.Clear();
    }
}
