using System.Collections.Generic;
using UnityEngine;

public class ClickSpawner : MonoBehaviour
{
    public GameObject GamePiece;
    public Camera Camera;
    public float spawnHeightOffset = 2.5f;

    private float pieceHeight;
    private Dictionary<Transform, int> poleStacks = new Dictionary<Transform, int>();

    void Start()
    {
        pieceHeight = GamePiece.GetComponent<Collider>().bounds.size.y;
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
        spawnPos.y += spawnHeightOffset + stackCount * pieceHeight;

        Instantiate(GamePiece, spawnPos, Quaternion.identity);

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
