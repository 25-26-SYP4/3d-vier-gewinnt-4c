using System;
using UnityEngine;

public class MouseHover : MonoBehaviour
{
    public Color hoverColor = Color.black;

    private Renderer lastRenderer;
    private Color originColor;

    private void Start()
    {
        Renderer startRend = GetComponentInChildren<Renderer>();
        if (startRend != null)
        {
            originColor = startRend.sharedMaterial.color;
        }
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform pole = GetPoleFromHit(hit.transform);

            if (pole != null)
            {
                Pole poleHover = pole.GetComponent<Pole>();
                Renderer rend = pole.GetComponent<Renderer>();

                if (rend != lastRenderer || (poleHover != null && poleHover.isFull))
                {
                    ClearLast();
                }

                if (poleHover != null && !poleHover.isFull && rend != lastRenderer)
                {
                    lastRenderer = rend;
                    lastRenderer.material.color = hoverColor;
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
            lastRenderer.material.color = originColor;
            lastRenderer = null;
        }
    }
}
