using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Pollution / Umweltverschmutzungs-Theme Setup.
/// - Setzt Smog-Stimmung (Fog, Ambient, Licht, Hintergrundfarbe).
/// - Baut den Smog-Hintergrund hinter das Spielfeld (Quad an der Kamera).
/// - Legt Fass-Embleme + Metallplatten auf die Spieler-Karten, End-Screen und Toast.
/// Laeuft im Editor UND im Play-Modus. Grafiken: Assets/Resources/PollutionTheme/
/// </summary>
[ExecuteAlways]
public class NeonBackgroundSetup : MonoBehaviour
{
    [Header("Hintergrundfarbe (Fallback hinter dem Bild)")]
    public Color backgroundColor = new Color(0.06f, 0.06f, 0.05f, 1f);
    public bool useSolidColor = true;

    [Header("Fog (Smog)")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.30f, 0.27f, 0.18f, 1f);
    public float fogDensity = 0.012f;

    [Header("Ambient Licht")]
    public Color ambientColor = new Color(0.18f, 0.17f, 0.12f, 1f);

    [Header("Directional Light")]
    public Light directionalLight;
    public Color lightColor = new Color(1.0f, 0.85f, 0.55f, 1f);
    public float lightIntensity = 0.9f;

    [Header("Pollution Theme")]
    public bool buildPollutionTheme = true;

    const string BG_NAME = "PollutionBackground";
    const string EMBLEM_NAME = "PollutionEmblem";

    void OnEnable()
    {
        Apply();
        ScheduleBuild();
    }

    void OnValidate()
    {
        Apply();
        ScheduleBuild();
    }

    void Start() => ScheduleBuild();

    void ScheduleBuild()
    {
        if (!buildPollutionTheme) return;

        if (Application.isPlaying)
        {
            StartCoroutine(BuildNextFrame());
        }
        else
        {
#if UNITY_EDITOR
            // Im Editor verzoegert ausfuehren (nach Import/Layout).
            EditorApplication.delayCall += () => { if (this != null) BuildTheme(); };
#endif
        }
    }

    IEnumerator BuildNextFrame()
    {
        yield return null; // ein Frame warten -> Canvas-Layout fertig
        BuildTheme();
    }

    void Apply()
    {
        Camera cam = Camera.main;
        if (cam != null && useSolidColor)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
        }

        RenderSettings.fog = enableFog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        if (directionalLight != null)
        {
            directionalLight.color = lightColor;
            directionalLight.intensity = lightIntensity;
        }
    }

    // -------- Theme-Aufbau --------

    void BuildTheme()
    {
        Canvas.ForceUpdateCanvases();
        bool bgOk = BuildBackground();
        StyleHud();
        Debug.Log($"[Pollution] Theme gebaut. Hintergrund geladen = {bgOk}");
    }

    bool BuildBackground()
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[Pollution] Keine Main Camera gefunden."); return false; }

        // alten Hintergrund entfernen (kein Duplikat)
        var old = GameObject.Find(BG_NAME);
        if (old != null) SafeDestroy(old);

        Texture2D bg = Resources.Load<Texture2D>("PollutionTheme/bg_pollution");
        if (bg == null)
        {
            Debug.LogWarning("[Pollution] bg_pollution NICHT gefunden in Resources/PollutionTheme!");
            return false;
        }

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = BG_NAME;
        if (!Application.isPlaying) quad.hideFlags = HideFlags.DontSave;
        var col = quad.GetComponent<Collider>();
        if (col != null) SafeDestroy(col);

        quad.transform.SetParent(cam.transform, false);
        float dist = Mathf.Min(cam.farClipPlane * 0.5f, 40f);
        quad.transform.localPosition = new Vector3(0f, 0f, dist);
        quad.transform.localRotation = Quaternion.identity;

        float height = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        quad.transform.localScale = new Vector3(height * aspect * 1.1f, height * 1.1f, 1f);

        // URP-sicher: zuerst URP/Unlit, sonst Fallback auf Sprites/Default
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Texture");
        var mat = new Material(sh);
        mat.mainTexture = bg;                                   // _MainTex
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", bg);   // URP/Unlit
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        var mr = quad.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return true;
    }

    Sprite ToSprite(string path)
    {
        Texture2D t = Resources.Load<Texture2D>(path);
        if (t == null)
        {
            Debug.LogWarning("[Pollution] Textur NICHT gefunden: " + path);
            return null;
        }
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    void StyleHud()
    {
        Sprite plate = ToSprite("PollutionTheme/plate");
        Sprite oil = ToSprite("PollutionTheme/icon_oil");
        Sprite toxic = ToSprite("PollutionTheme/icon_toxic");

        Game game = Object.FindFirstObjectByType<Game>();
        if (game != null)
        {
            // Player 1 = Giftmuell (gruen), Player 2 = Oel
            ApplyCard(game.player1Panel, plate, toxic);
            ApplyCard(game.player2Panel, plate, oil);

            if (game.endScreen != null)
            {
                Image img = game.endScreen.GetComponent<Image>();
                if (img != null)
                {
                    if (plate != null) img.sprite = plate;
                    img.color = new Color(0.10f, 0.10f, 0.08f, 0.97f);
                }
            }
        }
        else
        {
            Debug.LogWarning("[Pollution] Kein Game-Objekt gefunden (HUD nicht gestylt).");
        }

        ClickSpawner spawner = Object.FindFirstObjectByType<ClickSpawner>();
        if (spawner != null && spawner.messageGroup != null)
        {
            Image img = spawner.messageGroup.GetComponent<Image>();
            if (img != null)
            {
                if (plate != null) img.sprite = plate;
                img.color = new Color(0.45f, 0.12f, 0.07f, 0.95f); // Warn-Rot
            }
        }
    }

    void ApplyCard(Image panel, Sprite plate, Sprite emblem)
    {
        if (panel == null) return;

        if (plate != null)
        {
            panel.sprite = plate;
            panel.type = Image.Type.Simple;
        }

        if (emblem == null) return;

        // altes Emblem entfernen
        Transform oldEmblem = panel.transform.Find(EMBLEM_NAME);
        if (oldEmblem != null) SafeDestroy(oldEmblem.gameObject);

        float h = panel.rectTransform.rect.height;
        if (h < 10f) h = 50f;
        float size = h * 0.92f;

        var holder = new GameObject(EMBLEM_NAME, typeof(RectTransform), typeof(Image));
        if (!Application.isPlaying) holder.hideFlags = HideFlags.DontSave;
        var rt = holder.GetComponent<RectTransform>();
        rt.SetParent(panel.transform, false);
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(5f, 0f);

        var ei = holder.GetComponent<Image>();
        ei.sprite = emblem;
        ei.preserveAspect = true;
        ei.raycastTarget = false;
        ei.color = Color.white;
        rt.SetAsLastSibling();
    }

    void SafeDestroy(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }
}
