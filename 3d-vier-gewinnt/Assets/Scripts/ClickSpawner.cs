using System;
using System.Collections.Generic;
using UnityEngine;

public class ClickSpawner : MonoBehaviour
{
    public GameObject GamePiecePlayer1;
    public GameObject GamePiecePlayer2;
    public Camera Camera;
    public float spawnHeightOffset = 2.5f;

    private float pieceHeightPlayer1;
    private float pieceHeightPlayer2;
    private Dictionary<Transform, int> poleStacks = new Dictionary<Transform, int>();

    void Start()
    {
        pieceHeightPlayer1 = GamePiecePlayer1.GetComponent<Collider>().bounds.size.y;
        pieceHeightPlayer2 = GamePiecePlayer2.GetComponent<Collider>().bounds.size.y;
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

    void SpawnPieceOnPole(Transform pole)
    {
        if (!poleStacks.ContainsKey(pole))
            poleStacks[pole] = 0;

        int stackCount = poleStacks[pole];

        Vector3 spawnPos = pole.position;

        int poleY = int.Parse(pole.name.Replace("Pole", ""));
        int rowX = int.Parse(pole.parent.name.Substring(3));
        int heightZ = stackCount; 

        spawnPos.y += spawnHeightOffset + stackCount * pieceHeightPlayer1;

        Instantiate(GamePiecePlayer1, spawnPos, Quaternion.identity);

        poleStacks[pole]++;
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
}
