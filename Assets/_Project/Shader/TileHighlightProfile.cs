using UnityEngine;

public enum TileHoverKind
{
    None,
    PlayerUnit,          // A
    EmptyTile,           // B
    EnemyUnit,           // C
    EnemyAttackTarget,   // D
    Destructible         // F
}

[System.Serializable]
public struct HoverStyle
{
    public Color color;
    [Min(0f)] public float intensity;
}

[CreateAssetMenu(fileName = "HighlightProfile", menuName = "ScriptableObjects/Tiles/Highlight Profile")]
public class TileHighlightProfile : ScriptableObject
{
    [Header("Tile selezionata")]
    public Color selectedColor = Color.cyan;
    [Min(0f)] public float selectedIntensity = 3f;

    [Header("Tile del percorso")]
    public Color pathColor = Color.blue;
    [Min(0f)] public float pathIntensity = 1.5f;

    [Header("Hover")]
    public HoverStyle hoverPlayerUnit = new HoverStyle { color = Color.green, intensity = 2.5f };
    public HoverStyle hoverEmptyTile = new HoverStyle { color = Color.white, intensity = 1.5f };
    public HoverStyle hoverEnemyUnit = new HoverStyle { color = new Color(1f, 0.55f, 0f), intensity = 2.5f };
    public HoverStyle hoverEnemyAttackTarget = new HoverStyle { color = Color.red, intensity = 4f };
    public HoverStyle hoverDestructible = new HoverStyle { color = Color.yellow, intensity = 2.5f };

    public HoverStyle GetHover(TileHoverKind kind) => kind switch
    {
        TileHoverKind.PlayerUnit => hoverPlayerUnit,
        TileHoverKind.EmptyTile => hoverEmptyTile,
        TileHoverKind.EnemyUnit => hoverEnemyUnit,
        TileHoverKind.EnemyAttackTarget => hoverEnemyAttackTarget,
        TileHoverKind.Destructible => hoverDestructible,
        _ => default
    };
}