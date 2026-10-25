using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Cards/Pull", fileName = "Card_Pull")]
public class PullCard : CardData
{
    public int maxDistance = 4;
    public int pullDistance = 2;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.User == null || ctx.TargetUnit == null) return false;
        if (ctx.TargetUnit == ctx.User) return false;
        if (ctx.TargetUnit.Faction == ctx.User.Faction) return false;
        if (!ctx.TargetUnit.IsAlive) return false;
        return TileManager.Instance.GetDistance(ctx.User.Tile, ctx.TargetUnit.Tile) <= maxDistance;
    }

    public override IEnumerator Play(CardContext ctx)
    {
        if (!TrySpendCost(ctx)) yield break;

        Unit target = ctx.TargetUnit;
        Tile current = target.Tile;

        for (int i = 0; i < pullDistance; i++)
        {
            Vector2Int dir = new Vector2Int(
                Mathf.Clamp(ctx.User.Tile.Coord.x - current.Coord.x, -1, 1),
                Mathf.Clamp(ctx.User.Tile.Coord.y - current.Coord.y, -1, 1));

            Vector2Int nextCoord = current.Coord + dir;
            if (!TileManager.Instance.TryGetTile(nextCoord, out Tile next)) break;
            if (!next.IsWalkable || next.Occupant != null) break;
            if (next == ctx.User.Tile) break; // non può finire sopra l'utente

            current = next;
            yield return target.Mover.ForceMoveTo(next);
        }

        Debug.Log($"[PullCard] {target.name} attirato fino a {current.Coord}");
    }
}