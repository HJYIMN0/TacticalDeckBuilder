using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Attack")]
public class AttackCard : CardData
{
    public int maxDistance = 3;
    public int damage = 3;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.User == null || ctx.TargetTile == null) return false;
        Unit target = ctx.TargetUnit;
        if (target == null) return false;
        if (target == ctx.User) return false;
        if (target.Faction == ctx.User.Faction) return false;
        if (!target.IsAlive) return false;

        int dist = TileManager.Instance.GetDistance(ctx.User.Tile, ctx.TargetTile);
        return dist <= maxDistance;
    }

    public override IEnumerator Play(CardContext ctx)
    {
        Unit target = ctx.TargetUnit;
        Debug.Log($"[Attack] {ctx.User.name} -> {target.name} per {damage} danni");
        target.Health.TakeDamage(damage);
        yield return null;
    }
}