using UnityEngine;

public class MouseHover : MonoBehaviour
{
    public Color hoverColor = Color.yellow;

    private Renderer lastRenderer;
    private Color lastColor;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform pole = GetPoleFromHit(hit.transform);

            if (pole != null)
            {
                Pole poleHover = pole.GetComponent<Pole>();

                if (poleHover != null && poleHover.isFull)
                {
                    ClearLast();
                    return;
                }
                
                Renderer rend = pole.GetComponent<Renderer>();

                if (rend != null && rend != lastRenderer)
                {
                    ClearLast();

                    lastRenderer = rend;
                    lastColor = rend.material.color;

                    rend.material = new Material(rend.material);
                    rend.material.color = hoverColor;
                }
                return;
            }
        }

        ClearLast();
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

    void ClearLast()
    {
        if (lastRenderer != null)
        {
            lastRenderer.material.color = lastColor;
            lastRenderer = null;
        }
    }
}
