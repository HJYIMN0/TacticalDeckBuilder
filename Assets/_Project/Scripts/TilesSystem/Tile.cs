using UnityEngine;

public class Tile : MonoBehaviour
{
    // [MODIFICA] coord serializzata: la assegna TileGrid in editor e deve sopravvivere fuori dal Play (l'auto-property non veniva salvata)
    [SerializeField] Vector2Int coord;
    public Vector2Int Coord => coord;

    public Unit Occupant { get; private set; }

    [SerializeField] bool walkable = true;
    public bool IsWalkable => walkable && Occupant == null;
    public TileHighlight Highlight { get; private set; }
    public void SetCoord(Vector2Int coord) => this.coord = coord;

    public void SetOccupant(Unit unit)
    {
        if (unit != null && Occupant != null && Occupant != unit)
            Debug.LogWarning($"[Tile] {name} già occupata da {Occupant.name}, sovrascrivo con {unit.name}");

        Occupant = unit;
    }
    void Awake()
    {
        Highlight = GetComponent<TileHighlight>();
        if (Highlight == null)
            Debug.LogError($"[Tile] {name}: manca il componente TileHighlight sul prefab", this);
    }
}