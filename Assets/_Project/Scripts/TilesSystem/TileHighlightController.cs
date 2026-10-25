using System.Collections.Generic;
using UnityEngine;

public class TileHighlightController : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] TilePicker picker;
    [SerializeField] List<GridMover> playerMovers = new List<GridMover>();
    GridMover activeMover;

    [Header("Profili")]
    [SerializeField] TileHighlightProfile movementProfile;   // il tuo asset "PathMovment"
    [SerializeField] TileHighlightProfile attackProfile;     // il tuo asset "Attack"

    [Header("Distanza massima (in tile)")]
    [SerializeField, Min(1)] int movementRange = 3;

    [Header("Stato")]
    [SerializeField] bool suspended = false;

    TileHighlightProfile activeProfile;
    int activeRange;

    readonly List<Tile> highlighted = new List<Tile>();
    Tile lastHovered;
    Tile lastOrigin;

    public void ClearAllHighlights() => ClearHighlights();
    void Awake()
    {
        if (picker == null)
        {
            Debug.LogError("[TileHighlightController] assegna picker nell'Inspector", this);
            enabled = false;
            return;
        }

        SetMovementMode();
    }
    void Start()
    {
        RefreshPlayerMovers();
    }

    void OnDisable()
    {
        ClearHighlights();
    }

    void Update()
    {

        if (suspended) { ClearHighlights(); return; }
        if (activeMover == null) { ClearHighlights(); return; }
        // senza unità attiva non c'è un punto di partenza: spengo tutto
        if (activeMover == null)
        {
            ClearHighlights();
            return;
        }

        // durante il movimento spengo tutto e forzo il refresh a fine movimento
        if (activeMover.IsMoving) 
        {
            ClearHighlights();
            lastHovered = null;
            lastOrigin = null;
            return;
        }

        Tile hovered = picker.HoveredTile;
        Tile origin = activeMover.CurrentTile;

        // il percorso si ricalcola solo se cambia la tile puntata o quella di partenza (TryGetPath alloca)
        if (hovered == lastHovered && origin == lastOrigin) return;

        lastHovered = hovered;
        lastOrigin = origin;
        Refresh(hovered, origin);
    }
    /// <summary>
    /// cerca i GridMover con Faction.Player. Chiamalo di nuovo se spawni unità dopo Start()
    /// </summary>
    public void RefreshPlayerMovers()
    {
        playerMovers.Clear();

        // [MODIFICA] overload senza FindObjectsSortMode (deprecato): l'ordine dei risultati non è garantito
        GridMover[] all = FindObjectsByType<GridMover>();
        for (int i = 0; i < all.Length; i++)
        {
            Unit unit = all[i].GetComponent<Unit>();
            if (unit != null && unit.Faction == Faction.Player)
                playerMovers.Add(all[i]);
        }

        // se l'unità attiva non c'è più (o non era mai stata scelta) riparte dalla prima della lista
        if (activeMover == null || !playerMovers.Contains(activeMover))
            activeMover = playerMovers.Count > 0 ? playerMovers[0] : null;

        lastOrigin = null; // forza il refresh
        Debug.Log($"[TileHighlightController] Unit player trovate: {playerMovers.Count}, attiva: {(activeMover != null ? activeMover.name : "nessuna")}");
    }

    /// <summary>
    /// Cambia modo: profilo (colori/intensità) + distanza massima
    /// </summary>
    /// <param name="profile"></param>
    /// <param name="range"></param>
    public void SetMode(TileHighlightProfile profile, int range)
    {
        activeProfile = profile;
        activeRange = range;
        lastHovered = null; // forza il refresh al prossimo Update
    }

    public void SetMovementMode() => SetMode(movementProfile, movementRange);

    public void SetAttackMode(int range) => SetMode(attackProfile, range);

    public void SetActiveMover(GridMover mover)
    {
        activeMover = mover;
        lastOrigin = null; // forza il refresh
    }

    // Per provare i due modi senza altro codice: tre puntini sul componente, in Play
    [ContextMenu("Test: modo Movimento")]
    void TestMovementMode() => SetMovementMode();

    [ContextMenu("Test: modo Attacco")]
    void TestAttackMode() => SetAttackMode(movementRange);

    void Refresh(Tile hovered, Tile origin)
    {
        ClearHighlights();

        if (hovered == null || origin == null || activeProfile == null) return;
        if (hovered == origin) return;

        Unit activeUnit = activeMover.GetComponent<Unit>();
        bool isJump = activeUnit != null && activeUnit.HasAbility<JumpAbility>();
        Debug.Log($"[THC] unit={activeUnit?.name} isJump={isJump} " +
          $"abilities={(activeUnit != null ? activeUnit.Abilities.Count : -1)}");
        if (isJump)
        {
            // Salto: linea retta, ignora ostacoli e occupanti intermedi.
            // L'unico vincolo è che il target non sia un ostacolo/occupato,
            // ma quello lo valida MovementCard.IsValidTarget, non l'anteprima.
            var line = TileManager.Instance.GetLineTiles(origin, hovered);
            if (line.Count < 2) return;

            int range = activeRange;
            int dist = TileManager.Instance.GetDistance(origin, hovered);
            if (dist > range) return;

            for (int i = 1; i < line.Count - 1; i++)
                ShowOn(line[i], false);

            ShowOn(line[line.Count - 1], true);
            return;
        }

        // Comportamento standard
        if (!TileManager.Instance.TryGetPath(origin, hovered, activeRange, out List<Tile> path))
            return;

        for (int i = 1; i < path.Count - 1; i++)
            ShowOn(path[i], false);

        ShowOn(path[path.Count - 1], true);
    }

    void ShowOn(Tile tile, bool selected)
    {
        if (tile.Highlight == null) return;

        if (selected) tile.Highlight.ShowSelected(activeProfile);
        else tile.Highlight.ShowPath(activeProfile);

        highlighted.Add(tile);
    }

    void ClearHighlights()
    {
        for (int i = 0; i < highlighted.Count; i++)
        {
            Tile t = highlighted[i];
            if (t != null && t.Highlight != null) t.Highlight.Hide();
        }
        highlighted.Clear();
    }
    public void SetSuspended(bool value)
    {
        suspended = value;
        if (value) ClearHighlights();
    }
}