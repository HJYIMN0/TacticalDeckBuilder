using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int Coord { get; private set; }
    public Unit Occupant { get; private set; }

    [SerializeField] bool walkable = true;
    public bool IsWalkable => walkable && Occupant == null;

    public void SetCoord(Vector2Int coord) => Coord = coord;

    public void SetOccupant(Unit unit)
    {
        if (unit != null && Occupant != null && Occupant != unit)
            Debug.LogWarning($"[Tile] {name} già occupata da {Occupant.name}, sovrascrivo con {unit.name}");

        Occupant = unit;
    }

    void Start()
    {
        TileManager.Instance.Register(this);
    }
}