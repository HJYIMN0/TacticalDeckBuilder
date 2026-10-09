using UnityEngine;

[CreateAssetMenu(fileName = "HighlightProfile", menuName = "Tiles/Highlight Profile")]
public class TileHighlightProfile : ScriptableObject
{
    [Header("Tile selezionata")]
    public Color selectedColor = Color.cyan;
    [Min(0f)] public float selectedIntensity = 3f;

    [Header("Tile del percorso")]
    public Color pathColor = Color.blue;
    [Min(0f)] public float pathIntensity = 1.5f;
}