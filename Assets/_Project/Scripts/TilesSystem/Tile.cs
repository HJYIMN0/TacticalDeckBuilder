using UnityEngine;

public class Tile : MonoBehaviour
{
    // coord serializzata: la assegna TileGrid in editor e deve sopravvivere fuori dal Play (l'auto-property non veniva salvata)
    [SerializeField] Vector2Int coord;
    [Header("Terreno")]
    [Tooltip("Y del centro della tile. Dopo la modifica premi Generate sulla TileGrid.")]
    [SerializeField] float height;
    [Tooltip("Inclinazione in gradi. Ignorata se la direzione è None.")]
    [SerializeField, Range(0f, 70f)] float slopeAngle;
    [Tooltip("Verso cui la tile SALE.")]
    [SerializeField] SlopeDirection slopeDirection = SlopeDirection.None;
    // lato della tile, scritto da ApplyTerrain: serve a calcolare l'altezza dei bordi
    [SerializeField, HideInInspector] float size = 1f;

    public float Height => height;
    public float SlopeAngle => slopeDirection == SlopeDirection.None ? 0f : slopeAngle;


    public Vector2Int Coord => coord;
    public Unit Occupant { get; private set; }

    [SerializeField] bool walkable = true;
    public bool IsWalkable => walkable && Occupant == null;
    public TileHighlight Highlight { get; private set; }

    Vector2Int UphillDir
    {
        get
        {
            switch (slopeDirection)
            {
                case SlopeDirection.North: return Vector2Int.up;
                case SlopeDirection.East: return Vector2Int.right;
                case SlopeDirection.South: return Vector2Int.down;
                case SlopeDirection.West: return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }
    }
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

    /// <summary>
    /// Altezza (locale alla griglia) del punto medio del bordo verso 'dir'
    /// (dir = una delle 4 direzioni di griglia).
    /// </summary>
    public float GetEdgeHeight(Vector2Int dir)
    {
        Vector2Int up = UphillDir;
        if (up == Vector2Int.zero || slopeAngle <= 0f) return height;

        // +1 = bordo a monte, -1 = bordo a valle, 0 = bordo laterale (altezza del centro)
        int along = dir.x * up.x + dir.y * up.y;
        return height + along * (size * 0.5f) * Mathf.Tan(slopeAngle * Mathf.Deg2Rad);
    }

    /// <summary>
    /// Chiamato da TileGrid.Generate(): applica posizione, rotazione e scala.
    /// Sulle tile inclinate la scala è allungata di 1/cos(angolo) lungo l'asse di salita,
    /// così l'impronta orizzontale resta = tileSize e non si aprono buchi tra le tile.
    /// </summary>
    public void ApplyTerrain(float localX, float localZ, float tileSize, float planeBaseSize)
    {
        size = tileSize;

        Vector3 scale = Vector3.one * (tileSize / planeBaseSize);
        Quaternion rot = Quaternion.identity;

        if (slopeDirection != SlopeDirection.None && slopeAngle > 0f)
        {
            float stretch = 1f / Mathf.Cos(slopeAngle * Mathf.Deg2Rad);
            switch (slopeDirection)
            {
                case SlopeDirection.North: rot = Quaternion.Euler(-slopeAngle, 0f, 0f); scale.z *= stretch; break;
                case SlopeDirection.South: rot = Quaternion.Euler(slopeAngle, 0f, 0f); scale.z *= stretch; break;
                case SlopeDirection.East: rot = Quaternion.Euler(0f, 0f, slopeAngle); scale.x *= stretch; break;
                case SlopeDirection.West: rot = Quaternion.Euler(0f, 0f, -slopeAngle); scale.x *= stretch; break;
            }
        }

        transform.localPosition = new Vector3(localX, height, localZ);
        transform.localRotation = rot;
        transform.localScale = scale;
    }
}