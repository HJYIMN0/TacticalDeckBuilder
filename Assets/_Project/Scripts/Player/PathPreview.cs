using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathPreview : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TilePicker picker;

    [Header("Colors")]
    [SerializeField] Color reachableColor = Color.green;
    [SerializeField] Color unreachableColor = Color.red;
    [SerializeField] Color areaColor = new Color(1f, 0.6f, 0f, 1f); // arancione per AoE
    [SerializeField] float heightOffset = 0.2f;

    [Header("Fallback")]
    [Tooltip("Se un path non è calcolabile entro maxDistance, prova fino a questo limite per disegnarlo in rosso.")]
    [SerializeField] int hardCap = 20;

    LineRenderer line;
    Tile lastHovered;
    CardData currentCard;
    Unit executor;        // esecutore corrente (null per TileOnly)
    Unit forcedTarget;    // UnitOnly: unità target già scelta

    // -------------------------------------------------------------------
    // API pubblica — chiamata da PlayerController
    // -------------------------------------------------------------------

    /// <summary>Esecutore scelto (o null per TileOnly).</summary>
    public void SetUnit(Unit unit)
    {
        executor = unit;
        lastHovered = null;
        Hide();
    }

    /// <summary>Carta selezionata. Guida il tipo di preview mostrata.</summary>
    public void SetCard(CardData card)
    {
        currentCard = card;
        lastHovered = null;
        Hide();
    }

    /// <summary>UnitOnly: l'unità target è già stata scelta — mostra il marker dedicato.</summary>
    public void SetTargetUnit(Unit unit)
    {
        forcedTarget = unit;
        lastHovered = null;
        Hide();
        if (unit != null) ShowUnitTarget(unit);
    }

    /// <summary>Deseleziona la carta (ma conserva l'esecutore).</summary>
    public void ClearCard()
    {
        currentCard = null;
        lastHovered = null;
        Hide();
    }

    /// <summary>Reset completo — chiamato a fine turno.</summary>
    public void ClearAll()
    {
        currentCard = null;
        executor = null;
        forcedTarget = null;
        lastHovered = null;
        Hide();
    }

    // -------------------------------------------------------------------
    // Update
    // -------------------------------------------------------------------

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 0;
    }

    void Update()
    {
        if (picker == null || currentCard == null) { Hide(); return; }

        // UnitOnly: nessun hover, mostriamo solo il marker del target forzato.
        if (currentCard.TargetMode == CardTargeting.UnitOnly)
        {
            if (forcedTarget != null) ShowUnitTarget(forcedTarget);
            else Hide();
            return;
        }

        Tile hovered = picker.HoveredTile;
        if (hovered == null) { Hide(); return; }
        if (hovered == lastHovered) return;
        lastHovered = hovered;
        Show(hovered);
    }

    // -------------------------------------------------------------------
    // Dispatch per tipo di carta
    // -------------------------------------------------------------------

    void Show(Tile destination)
    {
        switch (currentCard.TargetMode)
        {
            case CardTargeting.TileOnly:
                ShowTileTarget(destination);
                return;

            case CardTargeting.ExecutorAndTile:
                if (executor == null || executor.Mover == null) { Hide(); return; }
                var mover = executor.Mover;
                if (mover.IsMoving || mover.CurrentTile == null) { Hide(); return; }
                if (destination == mover.CurrentTile) { Hide(); lastHovered = destination; return; }

                if (currentCard is MovementCard m) ShowMovement(mover, destination, m);
                else if (currentCard is AttackCard a) ShowAttack(mover, destination, a);
                else if (currentCard is AoeDamageCard e) ShowAoe(mover, destination, e);
                else if (currentCard is SelfDestructCard s) ShowSelfDestruct(mover, s);
                else line.positionCount = 0;
                return;

            default:
                line.positionCount = 0;
                return;
        }
    }

    // -------------------------------------------------------------------
    // Casi concreti
    // -------------------------------------------------------------------

    void ShowMovement(GridMover mover, Tile destination, MovementCard move)
    {
        if (TileManager.Instance.TryGetPath(mover.CurrentTile, destination, move.maxDistance, out var path))
        {
            DrawPath(path, reachableColor);
        }
        else if (TileManager.Instance.TryGetPath(mover.CurrentTile, destination, hardCap, out var full))
        {
            DrawPath(full, unreachableColor);
        }
        else
        {
            DrawLine(mover.CurrentTile, destination, unreachableColor);
        }
    }

    void ShowAttack(GridMover mover, Tile destination, AttackCard attack)
    {
        var ctx = new CardContext { User = executor, TargetTile = destination };
        bool valid = attack.IsValidTarget(ctx);
        DrawLine(mover.CurrentTile, destination, valid ? reachableColor : unreachableColor);
    }

    void ShowAoe(GridMover mover, Tile destination, AoeDamageCard aoe)
    {
        var ctx = new CardContext { User = executor, TargetTile = destination };
        bool valid = aoe.IsValidTarget(ctx);

        // Linea dall'esecutore al centro dell'area, colorata per validità.
        DrawLine(mover.CurrentTile, destination, valid ? areaColor : unreachableColor);

        // Il rendering dei tile dell'area è delegato a un futuro AreaPreviewController
        // opzionale (evita di accoppiare PathPreview alla logica interna di AoeDamageCard,
        // che espone `rows`/`columns` come private).
    }

    void ShowSelfDestruct(GridMover mover, SelfDestructCard self)
    {
        // Il target è l'esecutore stesso: nessun hover da seguire.
        // Per ora nessuna preview geometrica — la carta non ha un target selezionabile.
        line.positionCount = 0;
    }

    void ShowTileTarget(Tile tile)
    {
        // TileOnly: nessuna linea dall'esecutore (non esiste).
        // L'evidenziazione della tile è delegata a Tile.Highlight.
        line.positionCount = 0;
    }

    void ShowUnitTarget(Unit unit)
    {
        // UnitOnly: evidenzia l'unità target. Per ora nessuna linea:
        // l'evidenziazione visiva è delegata a UnitSelectorUI / un futuro marker.
        line.positionCount = 0;
    }

    // -------------------------------------------------------------------
    // Helper di disegno
    // -------------------------------------------------------------------

    void DrawPath(List<Tile> path, Color color)
    {
        line.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            line.SetPosition(i, ToLinePoint(path[i].transform.position));
        line.startColor = line.endColor = color;
    }

    void DrawLine(Tile from, Tile to, Color color)
    {
        if (from == null || to == null) { Hide(); return; }
        line.positionCount = 2;
        line.SetPosition(0, ToLinePoint(from.transform.position));
        line.SetPosition(1, ToLinePoint(to.transform.position));
        line.startColor = line.endColor = color;
    }

    void Hide()
    {
        line.positionCount = 0;
        lastHovered = null;
    }

    Vector3 ToLinePoint(Vector3 p) => new Vector3(p.x, p.y + heightOffset, p.z);

    public void ClearTargetUnit()
    {
        forcedTarget = null;
        Hide();
    }
}