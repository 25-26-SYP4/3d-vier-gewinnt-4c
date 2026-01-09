using UnityEngine;

public class MouseHover : MonoBehaviour
{
    private Renderer lastRenderer;
    private Color lastColor;

    public Color hoverColor = Color.yellow;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();

            if (rend != null && rend != lastRenderer)
            {
                ClearLast();

                lastRenderer = rend;
                lastColor = rend.material.color;
                rend.material.color = hoverColor;
            }
        }
        else
        {
            ClearLast();
        }
    }

    void ClearLast()
    {
        if (lastRenderer != null)
        {
            lastRenderer.material.color = lastColor;
            lastRenderer = null;
        }
    }
}
