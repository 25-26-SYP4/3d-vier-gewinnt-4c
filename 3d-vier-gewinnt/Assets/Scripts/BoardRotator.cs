using UnityEngine;

public class BoardRotator : MonoBehaviour
{
    public float rotationSpeed = 50f;   // Grad pro Sekunde (Pfeiltasten)
    public float mouseSpeed = 5f;       // Empfindlichkeit beim Ziehen mit rechter Maustaste

    private Quaternion startRotation;

    void Awake()
    {
        // Startausrichtung merken, damit ResetView() sie wiederherstellen kann.
        startRotation = transform.rotation;
    }

    void Update()
    {
        // --- Pfeiltasten: um beide Achsen frei drehen ---
        float yaw = 0f;    // links / rechts
        float pitch = 0f;  // hoch / runter

        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        // Space.World → immer relativ zur Kameraansicht drehen, egal wie das Board
        // gerade schon steht. Dadurch fühlt es sich "wie man will" drehbar an.
        transform.Rotate(Vector3.up, yaw * rotationSpeed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.right, pitch * rotationSpeed * Time.deltaTime, Space.World);

        // --- Rechte Maustaste gedrückt halten und ziehen: frei drehen ---
        // Rechte Taste, damit der Links-Klick weiter zum Steinsetzen frei bleibt.
        if (Input.GetMouseButton(1))
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            transform.Rotate(Vector3.up, mx * mouseSpeed, Space.World);
            transform.Rotate(Vector3.right, -my * mouseSpeed, Space.World);
        }
    }

    // Setzt die Ansicht auf die Startausrichtung zurück.
    // Optional an einen eigenen "Ansicht zurücksetzen"-Button hängen
    // (Button-OnClick → BoardRotator.ResetView).
    public void ResetView()
    {
        transform.rotation = startRotation;
    }
}
