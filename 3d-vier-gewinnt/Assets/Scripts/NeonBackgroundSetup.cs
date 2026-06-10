using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Neon Sci-Fi Hintergrund Setup
/// Attach dieses Script an ein leeres GameObject in der Scene.
/// Es setzt automatisch Hintergrundfarbe, Fog und Licht-Einstellungen.
/// </summary>
[ExecuteAlways]
public class NeonBackgroundSetup : MonoBehaviour
{
    [Header("Hintergrund")]
    public Color backgroundColor = new Color(0.02f, 0.02f, 0.08f, 1f);
    public bool useSolidColor = true;

    [Header("Fog (Atmosphäre)")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.03f, 0.04f, 0.12f, 1f);
    public float fogDensity = 0.015f;

    [Header("Ambient Licht")]
    public Color ambientColor = new Color(0.02f, 0.05f, 0.15f, 1f);

    [Header("Directional Light")]
    public Light directionalLight;
    public Color lightColor = new Color(0.6f, 0.8f, 1.0f, 1f);
    public float lightIntensity = 0.8f;

    void OnEnable() => Apply();

    void OnValidate() => Apply();

    void Apply()
    {
        // Hintergrundfarbe
        Camera cam = Camera.main;
        if (cam != null && useSolidColor)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
        }

        // Fog
        RenderSettings.fog = enableFog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        // Ambient Licht
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        // Directional Light
        if (directionalLight != null)
        {
            directionalLight.color = lightColor;
            directionalLight.intensity = lightIntensity;
        }
    }
}
