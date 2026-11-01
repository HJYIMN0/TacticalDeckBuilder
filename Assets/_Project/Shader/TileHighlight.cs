using UnityEngine;
using UnityEngine.Rendering;

public class TileHighlight : MonoBehaviour
{
    [SerializeField] Renderer edgeRenderer;

    static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
    static readonly int EdgeIntensityId = Shader.PropertyToID("_EdgeIntensity");

    MaterialPropertyBlock block;

    // Stato "base" (selected / path) e overlay hover
    bool baseVisible;
    Color baseColor;
    float baseIntensity;
    bool hoverVisible;

    void Awake()
    {
        block = new MaterialPropertyBlock();

        if (edgeRenderer == null)
        {
            Debug.LogError($"[TileHighlight] {name}: edgeRenderer non assegnato", this);
            return;
        }

        edgeRenderer.shadowCastingMode = ShadowCastingMode.Off;
        edgeRenderer.receiveShadows = false;
        edgeRenderer.enabled = false;
    }

    public void ShowSelected(TileHighlightProfile profile)
    {
        if (profile == null) return;
        SetBase(profile.selectedColor, profile.selectedIntensity);
    }

    public void ShowPath(TileHighlightProfile profile)
    {
        if (profile == null) return;
        SetBase(profile.pathColor, profile.pathIntensity);
    }

    public void Hide()
    {
        baseVisible = false;
        if (!hoverVisible && edgeRenderer != null) edgeRenderer.enabled = false;
    }

    public void ShowHover(TileHoverKind kind, TileHighlightProfile profile)
    {
        if (profile == null || kind == TileHoverKind.None) return;
        HoverStyle style = profile.GetHover(kind);
        hoverVisible = true;
        Apply(style.color, style.intensity);
    }

    public void ClearHover()
    {
        if (!hoverVisible) return;
        hoverVisible = false;

        if (baseVisible) Apply(baseColor, baseIntensity);
        else if (edgeRenderer != null) edgeRenderer.enabled = false;
    }

    void SetBase(Color color, float intensity)
    {
        baseVisible = true;
        baseColor = color;
        baseIntensity = intensity;
        if (!hoverVisible) Apply(color, intensity);
    }

    void Apply(Color color, float intensity)
    {
        if (edgeRenderer == null) return;

        edgeRenderer.GetPropertyBlock(block);
        block.SetColor(EdgeColorId, color);
        block.SetFloat(EdgeIntensityId, intensity);
        edgeRenderer.SetPropertyBlock(block);
        edgeRenderer.enabled = true;
    }
}