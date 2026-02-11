using UnityEngine;

public class BoardRotator : MonoBehaviour
{
    public float rotationSpeed = 50f;

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;

        transform.Rotate(vertical * rotationSpeed * Time.deltaTime,
                         horizontal * rotationSpeed * Time.deltaTime,
                         0f,
                         Space.Self);
    }
}