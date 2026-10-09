using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Movement")]
public class MovementCard : CardData
{
    public int maxDistance = 3;
    public LayerMask walkableLayerMask;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.User == null || ctx.TargetTile == null) return false;
        if (!ctx.TargetTile.IsWalkable) return false;
        if (ctx.TargetTile == ctx.User.Tile) return false;
        if (ctx.TargetTile.Occupant != null) return false;
        return TileManager.Instance.TryGetPath(ctx.User.Tile, ctx.TargetTile, maxDistance, out _);
    }

    public override IEnumerator Play(CardContext ctx)
    {
        yield return ctx.User.Mover.TryMoveAtTiles(ctx.TargetTile, maxDistance);
    }
}