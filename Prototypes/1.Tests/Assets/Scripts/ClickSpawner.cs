using System;
using UnityEngine;

public class ClickSpawner : MonoBehaviour
{
    public GameObject GamePiece;
    public Transform SpawnPoint;
    public Camera Camera;

    void Start()
    {
        
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
        Instantiate(GamePiece, SpawnPoint.position, Quaternion.identity);
    }
}
