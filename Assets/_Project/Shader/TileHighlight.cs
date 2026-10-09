using UnityEngine;
using UnityEngine.Rendering;

public class TileHighlight : MonoBehaviour
{
    [SerializeField] Renderer edgeRenderer;

    static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
    static readonly int EdgeIntensityId = Shader.PropertyToID("_EdgeIntensity");

    MaterialPropertyBlock block;

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
        Show(profile.selectedColor, profile.selectedIntensity);
    }

    public void ShowPath(TileHighlightProfile profile)
    {
        if (profile == null) return;
        Show(profile.pathColor, profile.pathIntensity);
    }

    public void Hide()
    {
        if (edgeRenderer != null) edgeRenderer.enabled = false;
    }

    void Show(Color color, float intensity)
    {
        if (edgeRenderer == null) return;

        edgeRenderer.GetPropertyBlock(block);
        block.SetColor(EdgeColorId, color);
        block.SetFloat(EdgeIntensityId, intensity);
        edgeRenderer.SetPropertyBlock(block);
        edgeRenderer.enabled = true;
    }
}