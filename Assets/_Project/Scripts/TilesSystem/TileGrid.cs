using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TileGrid : MonoBehaviour
{
    [Header("Griglia")]
    [SerializeField] Tile tilePrefab;
    [SerializeField, Min(1)] int width = 5;
    [SerializeField, Min(1)] int height = 5;
    [SerializeField, Min(0.01f)] float tileSize = 1f;
    [SerializeField, Min(0f)] float gap = 0f;
    const float PlaneBaseSize = 10f;
    // distanza tra i centri di due tile adiacenti
    float CellSize => tileSize + gap;
    [Header("Tile create (selezionale per modificarle)")]
    // [MODIFICA] tooltip: spiega come sostituire una tile
    [Tooltip("Per sostituire una tile: trascina un prefab dal Project sullo slot e premi Generate. Per resettarla: imposta None. Non rimuovere elementi e non riordinarli.")]
    [SerializeField] List<Tile> tiles = new List<Tile>();

    // larghezza usata nell'ultima generazione: serve a ricavare la coord di ogni slot dall'indice nella lista
    [SerializeField, HideInInspector] int generatedWidth;
    public IReadOnlyList<Tile> Tiles => tiles;

    void Awake()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] == null) continue;
            TileManager.Instance.Register(tiles[i]);
        }
        Debug.Log($"[TileGrid] Registrate {tiles.Count} tile");
    }

#if UNITY_EDITOR

    Tile Spawn(Tile prefab)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, transform);
        Undo.RegisterCreatedObjectUndo(go, "Create Tile");
        return go.GetComponent<Tile>();
    }
    public void Generate()
    {
        if (tilePrefab == null || !PrefabUtility.IsPartOfPrefabAsset(tilePrefab))
        {
            Debug.LogError("[TileGrid] tilePrefab deve essere un prefab asset del Project", this);
            return;
        }

        Undo.RegisterCompleteObjectUndo(this, "Generate TileGrid");

        // [MODIFICA] lo slot è l'indice nella lista (larghezza dell'ultima generazione), non più t.Coord
        int oldWidth = generatedWidth > 0 ? generatedWidth : width;
        var slots = new Dictionary<Vector2Int, Tile>();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] == null) continue;
            slots[new Vector2Int(i % oldWidth, i / oldWidth)] = tiles[i];
        }

        var usedInstances = new HashSet<Tile>();
        var result = new List<Tile>(width * height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var coord = new Vector2Int(x, y);
                slots.TryGetValue(coord, out Tile entry);

                // [MODIFICA] 4 casi per lo slot: vuoto, prefab asset, istanza ripetuta, istanza di scena
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
                    Debug.LogWarning($"[TileGrid] {entry.name} è in più slot della lista: per {coord} creo una tile nuova", this);
                    tile = Spawn(tilePrefab);
                }
                else
                {
                    tile = entry;
                    if (tile.transform.parent != transform)
                        Undo.SetTransformParent(tile.transform, transform, "Reparent Tile");
                }

                Undo.RecordObject(tile.transform, "Position Tile");
                // [MODIFICA] Undo su Tile spostato qui: ApplyTerrain scrive anche il campo 'size' del Tile
                Undo.RecordObject(tile, "Set Tile Coord");
                // [MODIFICA] posizione/rotazione/scala ora vengono da Tile (altezza e pendenza), non più Y = 0 fissa
                tile.ApplyTerrain(x * CellSize, y * CellSize, tileSize, PlaneBaseSize);
                tile.SetCoord(coord);
                tile.name = $"Tile ({x},{y})";
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tile);

                result.Add(tile);
            }
        }
        var keep = new HashSet<Tile>(result);
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Tile child = transform.GetChild(i).GetComponent<Tile>();
            if (child != null && !keep.Contains(child))
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        tiles = result;
        generatedWidth = width; // [MODIFICA] memorizzata per la prossima generazione
        EditorUtility.SetDirty(this);
        Debug.Log($"[TileGrid] Griglia {width}x{height}: {tiles.Count} tile, tileSize {tileSize}, gap {gap}");
    }

    public void Clear()
    {
        Undo.RegisterCompleteObjectUndo(this, "Clear TileGrid");

        foreach (Tile t in tiles)
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);

        tiles = new List<Tile>();
        EditorUtility.SetDirty(this);
        Debug.Log("[TileGrid] Griglia svuotata");
    }
#endif
}