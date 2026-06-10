using UnityEngine;

/// <summary>
/// Ersetzt MouseHover.cs - gleiche Logik aber mit Neon-Glow Hover-Effekt.
/// Einfach das alte MouseHover Script durch dieses ersetzen (oder umbenennen).
/// </summary>
public class MouseHover : MonoBehaviour
{
    // Hover-Farbe: helles Cyan-Neon beim Drüberfahren
    public Color hoverColor = new Color(0.0f, 1.0f, 1.0f, 1f);

    private Renderer lastRenderer;
    private Color originColor;
    private Color originEmission;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        Renderer startRend = GetComponentInChildren<Renderer>();
        if (startRend != null)
        {
            originColor = startRend.sharedMaterial.color;
            // Hol Original-Emission falls vorhanden
            if (startRend.sharedMaterial.HasProperty(EmissionColor))
                originEmission = startRend.sharedMaterial.GetColor(EmissionColor);
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
                    // Instanz-Material verwenden um SharedMaterial nicht zu ändern
                    lastRenderer.material.color = hoverColor;
                    if (lastRenderer.material.HasProperty(EmissionColor))
                        lastRenderer.material.SetColor(EmissionColor, hoverColor * 0.8f);
                    if (lastRenderer.material.HasProperty(BaseColor))
                        lastRenderer.material.SetColor(BaseColor, hoverColor);
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
            if (lastRenderer.material.HasProperty(EmissionColor))
                lastRenderer.material.SetColor(EmissionColor, originEmission);
            if (lastRenderer.material.HasProperty(BaseColor))
                lastRenderer.material.SetColor(BaseColor, originColor);
            lastRenderer = null;
        }
    }
}
