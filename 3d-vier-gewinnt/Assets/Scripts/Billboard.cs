using UnityEngine;

/// <summary>
/// Dreht das Objekt jeden Frame zur Kamera (Billboard).
/// Wird auf die Fass-Sprites der Spielsteine gelegt.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Aufrecht bleiben, nur seitlich (Yaw) zur Kamera drehen
        Vector3 dir = transform.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
