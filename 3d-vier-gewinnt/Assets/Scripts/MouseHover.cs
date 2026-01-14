using UnityEngine;

public class MouseHover : MonoBehaviour
{
    public Color hoverColor = Color.yellow;

    private Renderer lastRenderer;
    private Color lastColor;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Transform hitTransform = hit.transform;

            if (IsPole(hitTransform))
            {
                Renderer rend = hitTransform.GetComponent<Renderer>();

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

    bool IsPole(Transform t)
    {

        if (!t.name.StartsWith("Pole"))
            return false;

        return t.IsChildOf(transform);
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
