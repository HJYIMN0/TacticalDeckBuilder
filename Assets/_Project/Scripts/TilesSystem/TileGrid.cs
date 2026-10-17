using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// [NUOVO] una voce per ogni coordinata della griglia. tile == null → tile cancellata: Generate non la ricrea.
[System.Serializable]
public class TileSlot
{
    public Vector2Int coord;
    public Tile tile;
}

// [NUOVO] riga con un numero di colonne diverso dal default
[System.Serializable]
public class RowOverride
{
    [Min(0)] public int row;
    [Min(0)] public int columns;
}

public class TileGrid : MonoBehaviour
{
    [Header("Griglia")]
    [SerializeField] Tile tilePrefab;
    [Tooltip("Colonne di default di ogni riga")]
    [SerializeField, Min(1)] int width = 5;
    [Tooltip("Numero di righe")]
    [SerializeField, Min(1)] int height = 5;
    [SerializeField, Min(0.01f)] float tileSize = 1f;
    [SerializeField, Min(0f)] float gap = 0f;

    // [NUOVO]
    [Tooltip("Righe con un numero di colonne diverso dal default. Le righe si numerano da 0. Es.: riga 8 → 7 colonne.")]
    [SerializeField] List<RowOverride> rowOverrides = new List<RowOverride>();

    const float PlaneBaseSize = 10f;
    // distanza tra i centri di due tile adiacenti
    float CellSize => tileSize + gap;

    [Header("Tile create")]
    // [MODIFICA] ogni voce ha la sua coord: l'ordine della lista non conta più
    [Tooltip("Per sostituire una tile: trascina un prefab dal Project nel campo e premi Generate. Per cancellarla: eliminala dalla scena (resta cancellata). Per ricrearla: Ricrea / Add.")]
    [SerializeField] List<TileSlot> slots = new List<TileSlot>();

    // [MODIFICA] vecchia lista + larghezza: tenute SOLO per migrare le scene già generate
    [SerializeField, HideInInspector] List<Tile> tiles = new List<Tile>();
    [SerializeField, HideInInspector] int generatedWidth;

    readonly List<Tile> tilesCache = new List<Tile>();

    // [MODIFICA] stessa API di prima, ora costruita dagli slot con una tile
    public IReadOnlyList<Tile> Tiles
    {
        get
        {
            if (tilesCache.Count == 0) RebuildCache();
            return tilesCache;
        }
    }

    // [NUOVO] usata dall'Inspector custom
    public IReadOnlyList<TileSlot> Slots => slots;

    // [NUOVO] colonne di una riga: override se presente, altrimenti width
    public int GetColumns(int row)
    {
        for (int i = 0; i < rowOverrides.Count; i++)
            if (rowOverrides[i].row == row) return rowOverrides[i].columns;
        return width;
    }

    // [NUOVO]
    public bool IsInLayout(Vector2Int coord)
    {
        return coord.y >= 0 && coord.y < height && coord.x >= 0 && coord.x < GetColumns(coord.y);
    }

    void Awake()
    {
        MigrateLegacyTiles();
        RebuildCache();

        for (int i = 0; i < tilesCache.Count; i++)
            TileManager.Instance.Register(tilesCache[i]);

        Debug.Log($"[TileGrid] Registrate {tilesCache.Count} tile");
    }

    // [NUOVO] converte la vecchia lista (indice → coord) negli slot. Gli elementi null (tile cancellate) restano cancellati.
    void MigrateLegacyTiles()
    {
        if (slots.Count > 0 || tiles.Count == 0) return;

        int oldWidth = generatedWidth > 0 ? generatedWidth : width;
        for (int i = 0; i < tiles.Count; i++)
            slots.Add(new TileSlot { coord = new Vector2Int(i % oldWidth, i / oldWidth), tile = tiles[i] });

        tiles.Clear();
        generatedWidth = 0;
    }

    // [NUOVO]
    void RebuildCache()
    {
        tilesCache.Clear();
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].tile != null) tilesCache.Add(slots[i].tile);
    }

#if UNITY_EDITOR

    bool HasValidPrefab()
    {
        if (tilePrefab != null && PrefabUtility.IsPartOfPrefabAsset(tilePrefab)) return true;

        Debug.LogError("[TileGrid] tilePrefab deve essere un prefab asset del Project", this);
        return false;
    }

    Tile Spawn(Tile prefab)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, transform);
        Undo.RegisterCreatedObjectUndo(go, "Create Tile");
        return go.GetComponent<Tile>();
    }

    // [NUOVO] posizione/coord/nome di una tile (prima era inline in Generate). La chiamata a ApplyTerrain è invariata.
    void PlaceTile(Tile tile, Vector2Int coord)
    {
        Undo.RecordObject(tile.transform, "Position Tile");
        Undo.RecordObject(tile, "Set Tile Coord");
        tile.ApplyTerrain(coord.x * CellSize, coord.y * CellSize, tileSize, PlaneBaseSize);
        tile.SetCoord(coord);
        tile.name = $"Tile ({coord.x},{coord.y})";
        PrefabUtility.RecordPrefabInstancePropertyModifications(tile.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(tile);
    }

    // [NUOVO] slot ordinati per riga e poi per colonna (l'Inspector raggruppa per riga)
    void SortSlots()
    {
        slots.Sort((a, b) => a.coord.y != b.coord.y
            ? a.coord.y.CompareTo(b.coord.y)
            : a.coord.x.CompareTo(b.coord.x));
    }

    TileSlot FindSlot(Vector2Int coord)
    {
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].coord == coord) return slots[i];
        return null;
    }

    public void Generate()
    {
        if (!HasValidPrefab()) return;

        Undo.RegisterCompleteObjectUndo(this, "Generate TileGrid");
        MigrateLegacyTiles();

        // [MODIFICA] gli slot si ritrovano per coordinata salvata, non per indice
        var existing = new Dictionary<Vector2Int, TileSlot>();
        foreach (TileSlot s in slots) existing[s.coord] = s;

        var usedInstances = new HashSet<Tile>();
        var result = new List<TileSlot>();
        int keptDeleted = 0;

        for (int y = 0; y < height; y++)
        {
            int columns = GetColumns(y); // [MODIFICA] righe di lunghezza diversa
            for (int x = 0; x < columns; x++)
            {
                var coord = new Vector2Int(x, y);
                bool existed = existing.TryGetValue(coord, out TileSlot slot);

                // [NUOVO] slot già noto con tile null = cancellata: resta cancellata
                if (existed && slot.tile == null)
                {
                    result.Add(slot);
                    keptDeleted++;
                    continue;
                }

                if (!existed) slot = new TileSlot { coord = coord };
                Tile entry = slot.tile; // null solo per slot nuovi

                // 4 casi per lo slot: nuovo, prefab asset, istanza ripetuta, istanza di scena
                Tile tile;
                if (entry == null)
                {
                    tile = Spawn(tilePrefab);
                }
                else if (PrefabUtility.IsPartOfPrefabAsset(entry))
                {
                    // un prefab asset nello slot = "metti questo prefab qui": si istanzia, l'asset non si tocca
                    Debug.Log($"[TileGrid] Slot {coord}: sostituita con il prefab {entry.name}");
                    tile = Spawn(entry);
                }
                else if (!usedInstances.Add(entry))
                {
                    Debug.LogWarning($"[TileGrid] {entry.name} è in più slot: per {coord} creo una tile nuova", this);
                    tile = Spawn(tilePrefab);
                }
                else
                {
                    tile = entry;
                    if (tile.transform.parent != transform)
                        Undo.SetTransformParent(tile.transform, transform, "Reparent Tile");
                }

                slot.tile = tile;
                PlaceTile(tile, coord);
                result.Add(slot);
            }
        }

        // distrugge le Tile figlie non più in nessuno slot (slot sostituiti, tile fuori dalla nuova dimensione)
        var keep = new HashSet<Tile>();
        foreach (TileSlot s in result)
            if (s.tile != null) keep.Add(s.tile);

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Tile child = transform.GetChild(i).GetComponent<Tile>();
            if (child != null && !keep.Contains(child))
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        slots = result;
        RebuildCache();
        EditorUtility.SetDirty(this);
        Debug.Log($"[TileGrid] Generate: {keep.Count} tile presenti, {keptDeleted} cancellate mantenute (tileSize {tileSize}, gap {gap})");
    }

    // [NUOVO] crea (o ricrea, se era stata cancellata) la tile alla coordinata data. Solo dentro la griglia.
    public Tile AddTile(Vector2Int coord)
    {
        if (!HasValidPrefab()) return null;

        if (!IsInLayout(coord))
        {
            Debug.LogWarning($"[TileGrid] {coord} è fuori dalla griglia: aumenta height, width o l'override della riga {coord.y}", this);
            return null;
        }

        TileSlot slot = FindSlot(coord);
        if (slot != null && slot.tile != null)
        {
            Debug.Log($"[TileGrid] {coord}: la tile esiste già ({slot.tile.name})", this);
            return slot.tile;
        }

        Undo.RegisterCompleteObjectUndo(this, "Add Tile");

        if (slot == null)
        {
            slot = new TileSlot { coord = coord };
            slots.Add(slot);
            SortSlots();
        }

        Tile tile = Spawn(tilePrefab);
        slot.tile = tile;
        PlaceTile(tile, coord);

        RebuildCache();
        EditorUtility.SetDirty(this);
        Debug.Log($"[TileGrid] Tile creata in {coord}");
        return tile;
    }

    public void Clear()
    {
        Undo.RegisterCompleteObjectUndo(this, "Clear TileGrid");

        foreach (TileSlot s in slots)
        {
            // [MODIFICA] mai distruggere un prefab asset eventualmente trascinato in uno slot
            if (s.tile != null && !PrefabUtility.IsPartOfPrefabAsset(s.tile))
                Undo.DestroyObjectImmediate(s.tile.gameObject);
        }

        slots = new List<TileSlot>(); // si dimenticano anche le tile cancellate
        RebuildCache();
        EditorUtility.SetDirty(this);
        Debug.Log("[TileGrid] Griglia svuotata (dimenticate anche le tile cancellate)");
    }
#endif
}