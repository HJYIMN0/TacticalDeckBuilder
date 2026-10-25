using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Cards/Movement", fileName = "Card_Movement")]
public class MovementCard : CardData
{
    public int maxDistance = 3;
    public LayerMask walkableLayerMask;

    public override string Description => string.IsNullOrEmpty(description) ? "move up to " + maxDistance + " tiles" : description;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.User == null || ctx.TargetTile == null) return false;
        if (ctx.TargetTile == ctx.User.Tile) return false;

        // Target deve essere sempre libero e calpestabile, sia per jump sia per walk
        if (!ctx.TargetTile.IsWalkable) return false;
        if (ctx.TargetTile.Occupant != null) return false;

        var jump = ctx.User.GetAbility<JumpAbility>();
        if (jump != null)
        {
            // Jump: solo distanza Manhattan, ignora completamente il path
            int range = Mathf.Max(1, Mathf.RoundToInt(maxDistance * jump.rangeMultiplier));
            int dist = TileManager.Instance.GetDistance(ctx.User.Tile, ctx.TargetTile);
            return dist <= range;
        }

        bool trample = ctx.User.HasAbility<TrampleAbility>();
        return TileManager.Instance.TryGetPath(
            ctx.User.Tile, ctx.TargetTile, maxDistance, out _, ctx.User, trample);
    }
    public override IEnumerator Play(CardContext ctx)
    {
        if (!TrySpendCost(ctx)) yield break;

        if (ctx.User.HasAbility<JumpAbility>())
            yield return ctx.User.Mover.JumpTo(ctx.TargetTile);
        else
            yield return ctx.User.Mover.TryMoveAtTiles(ctx.TargetTile, maxDistance);
    }
}