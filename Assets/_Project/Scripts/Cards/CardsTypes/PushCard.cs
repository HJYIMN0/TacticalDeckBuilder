using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Cards/Push", fileName = "Card_Push")]
public class PushCard : CardData
{
    public int maxDistance = 2;
    public int pushDistance = 2;

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
        Vector2Int dir = new Vector2Int(
            Mathf.Clamp(target.Tile.Coord.x - ctx.User.Tile.Coord.x, -1, 1),
            Mathf.Clamp(target.Tile.Coord.y - ctx.User.Tile.Coord.y, -1, 1));

        for (int i = 0; i < pushDistance; i++)
        {
            Vector2Int nextCoord = current.Coord + dir;
            if (!TileManager.Instance.TryGetTile(nextCoord, out Tile next)) break;
            if (!next.IsWalkable || next.Occupant != null) break;

            current = next;
            yield return target.Mover.ForceMoveTo(next);
        }

        Debug.Log($"[PushCard] {target.name} spinto fino a {current.Coord}");
    }
}