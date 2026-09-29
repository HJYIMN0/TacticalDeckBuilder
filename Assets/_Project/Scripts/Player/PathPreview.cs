using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathPreview : MonoBehaviour
{
    [SerializeField] TilePicker picker;
    [SerializeField] GridMover mover;
    [SerializeField] Color reachableColor = Color.green;
    [SerializeField] Color unreachableColor = Color.red;
    [SerializeField] float heightOffset = 0.2f;
    [SerializeField] int hardCap = 20; // tetto per la BFS "esplorativa" del preview

    LineRenderer line;
    Tile lastHovered;
    CardData currentCard;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 0;
        if (mover == null) mover = GetComponent<GridMover>();
    }

    public void SetCard(CardData card)
    {
        currentCard = card;
        lastHovered = null;
    }

    public void ClearCard()
    {
        currentCard = null;
        Hide();
    }

    void Update()
    {
        if (mover == null || mover.IsMoving || mover.CurrentTile == null) { Hide(); return; }
        if (picker == null || currentCard == null) { Hide(); return; }

        Tile hovered = picker.HoveredTile;
        if (hovered == null) { Hide(); return; }
        if (hovered == lastHovered) return;
        lastHovered = hovered;

        Show(hovered);
    }

    void Show(Tile destination)
    {
        if (destination == mover.CurrentTile) { Hide(); lastHovered = destination; return; }

        if (currentCard is MovementCard move)
        {
            ShowMovement(destination, move.maxDistance);
        }
        else if (currentCard is AttackCard attack)
        {
            ShowAttack(destination, attack);
        }
        else
        {
            line.positionCount = 0;
        }
    }

    void ShowMovement(Tile destination, int maxDist)
    {
        if (TileManager.Instance.TryGetPath(mover.CurrentTile, destination, maxDist, out var path))
        {
            DrawPath(path, reachableColor);
            return;
        }

        if (TileManager.Instance.TryGetPath(mover.CurrentTile, destination, hardCap, out var fullPath))
            DrawPath(fullPath, unreachableColor);
        else
            line.positionCount = 0;
    }

    void ShowAttack(Tile destination, AttackCard attack)
    {
        Unit user = mover.GetComponent<Unit>();
        var ctx = new CardContext { User = user, TargetTile = destination };
        bool valid = attack.IsValidTarget(ctx);

        line.positionCount = 2;
        line.SetPosition(0, ToLinePoint(mover.CurrentTile.transform.position));
        line.SetPosition(1, ToLinePoint(destination.transform.position));
        line.startColor = line.endColor = valid ? reachableColor : unreachableColor;
    }

    void DrawPath(List<Tile> path, Color color)
    {
        line.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            line.SetPosition(i, ToLinePoint(path[i].transform.position));
        line.startColor = line.endColor = color;
    }

    int GetMaxDistance(CardData card)
    {
        if (card is MovementCard m) return m.maxDistance;
        return 0;
    }

    void Hide()
    {
        line.positionCount = 0;
        lastHovered = null;
    }

    Vector3 ToLinePoint(Vector3 p)
    {
        return new Vector3(p.x, transform.position.y + heightOffset, p.z);
    }
}