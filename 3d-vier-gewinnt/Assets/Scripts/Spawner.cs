using System;
using UnityEngine;

public class Spawner : MonoBehaviour
{

    public GameObject GamePiece;
    public Transform SpawnPoint;
    void Start()
    {
        SpawnPiece();
    }

    private void SpawnPiece()
    {
        Instantiate(GamePiece, SpawnPoint.position, Quaternion.identity);
    }
}
