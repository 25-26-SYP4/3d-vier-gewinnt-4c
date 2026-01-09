using UnityEngine;

public class ClickSpawner : MonoBehaviour
{
    public GameObject GamePiece;
    public Transform SpawnPoint;
    public Camera Camera;

    private int stackCount = 0;
    private float pieceHeight;

    void Start()
    {
        pieceHeight = GamePiece.GetComponent<Collider>().bounds.size.y;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == SpawnPoint)
                {
                    SpawnPiece();
                }
            }
        }
    }

    private void SpawnPiece()
    {
        Vector3 spawnPos = SpawnPoint.position;
        spawnPos.y += stackCount * pieceHeight;

        Instantiate(GamePiece, spawnPos, Quaternion.identity);
        stackCount++;
    }
}
