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
    [SerializeField] Color areaColor = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] float heightOffset = 0.2f;

    [Header("Fallback")]
    [SerializeField] int hardCap = 20;

    LineRenderer line;
    Tile lastHovered;
    CardData currentCard;
    Unit executor;
    Unit forcedTarget;

    // -------------------------------------------------------------------
    // API pubblica
    // -------------------------------------------------------------------

    public void SetUnit(Unit unit)
    {
        executor = unit;
        lastHovered = null;
        Hide();
    }

    public void SetCard(CardData card)
    {
        currentCard = card;
        lastHovered = null;
        Hide();
    }

    public void SetTargetUnit(Unit unit)
    {
        forcedTarget = unit;
        lastHovered = null;
        Hide();
        if (unit != null) ShowUnitTarget(unit);
    }

    public void ClearCard()
    {
        currentCard = null;
        lastHovered = null;
        Hide();
    }

    public void ClearAll()
    {
        currentCard = null;
        executor = null;
        forcedTarget = null;
        lastHovered = null;
        Hide();
    }

    public void ClearTargetUnit()
    {
        forcedTarget = null;
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
                if (destination == mover.CurrentTile) { Hide(); return; }

                if (currentCard is MovementCard m) ShowMovement(mover, destination, m);
                else if (currentCard is AbstractAttackCard a) ShowAttack(mover, destination, a);
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
        Unit u = executor;

        // --- CASO JUMP: linea retta, ignora ostacoli e occupanti intermedi ---
        var jump = u != null ? u.GetAbility<JumpAbility>() : null;
        if (jump != null)
        {
            var straight = TileManager.Instance.GetLineTiles(mover.CurrentTile, destination);
            if (straight.Count < 2) { Hide(); return; }

            int range = Mathf.Max(1, Mathf.RoundToInt(move.maxDistance * jump.rangeMultiplier));
            int dist = TileManager.Instance.GetDistance(mover.CurrentTile, destination);
            bool inRange = dist <= range;

            // Target non può essere occupato o ostacolo: colora rosso se invalido.
            bool landable = destination.IsWalkable && destination.Occupant == null;

            DrawPath(straight, (inRange && landable) ? reachableColor : unreachableColor);
            return;
        }

        // --- CASO WALK (con eventuale trample) ---
        bool trample = u != null && u.HasAbility<TrampleAbility>();

        if (TileManager.Instance.TryGetPath(
                mover.CurrentTile, destination, move.maxDistance, out var path, u, trample))
        {
            DrawPath(path, reachableColor);
        }
        else if (TileManager.Instance.TryGetPath(
                mover.CurrentTile, destination, hardCap, out var full, u, trample))
        {
            DrawPath(full, unreachableColor);
        }
        else
        {
            DrawLine(mover.CurrentTile, destination, unreachableColor);
        }
    }

    void ShowAttack(GridMover mover, Tile destination, AbstractAttackCard attack)
    {
        var ctx = new CardContext { User = executor, TargetTile = destination };
        bool valid = attack.IsValidTarget(ctx);
        DrawLine(mover.CurrentTile, destination, valid ? reachableColor : unreachableColor);
    }

    void ShowAoe(GridMover mover, Tile destination, AoeDamageCard aoe)
    {
        var ctx = new CardContext { User = executor, TargetTile = destination };
        bool valid = aoe.IsValidTarget(ctx);
        DrawLine(mover.CurrentTile, destination, valid ? areaColor : unreachableColor);
    }

    void ShowSelfDestruct(GridMover mover, SelfDestructCard self)
    {
        line.positionCount = 0;
    }

    void ShowTileTarget(Tile tile)
    {
        line.positionCount = 0;
    }

    void ShowUnitTarget(Unit unit)
    {
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
}