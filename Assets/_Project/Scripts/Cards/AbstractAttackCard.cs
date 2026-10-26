using System.Collections;
using UnityEngine;

public abstract class AbstractAttackCard : CardData
{
    public int maxDistance = 3;
    public int damage = 3;
    public LayerMask blockingMask;
    public float rayHeight = 0.5f;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.User == null || ctx.TargetTile == null) return false;
        Unit target = ctx.TargetUnit;
        if (target == null) return false;
        if (target == ctx.User) return false;
        if (target.Faction == ctx.User.Faction) return false;
        if (!target.IsAlive) return false;
        return TileManager.Instance.GetDistance(ctx.User.Tile, ctx.TargetTile) <= maxDistance;
    }

    public override bool CanAfford(CardContext ctx)
    {
        int total = cost + GetExtraCost(ctx.User);
        return ctx.Energy != null && ctx.Energy.CanSpend(total);
    }

    protected override bool TrySpendCost(CardContext ctx)
    {
        if (ctx.User != null && ctx.User.Faction == Faction.Enemy)
            return IsValidTarget(ctx);

        if (!CanPlay(ctx)) return false;

        return ctx.Energy.Spend(cost + GetExtraCost(ctx.User));
    }

    protected virtual int GetExtraCost(Unit user) => 0;

    protected virtual int GetAttackCount(CardContext ctx) => 1;

    public override IEnumerator Play(CardContext ctx)
    {
        if (!TrySpendCost(ctx)) yield break;

        int attacks = GetAttackCount(ctx);
        for (int i = 0; i < attacks; i++)
        {
            if (ctx.TargetTile == null) break;
            Unit target = ResolveTarget(ctx);
            if (target == null || !target.IsAlive) break;

            Debug.Log($"[Attack] {ctx.User.name} -> {target.name} per {damage} danni (colpo {i + 1}/{attacks})");
            target.Health.TakeDamage(damage, ctx.User);
            yield return null;
        }
    }

    Unit ResolveTarget(CardContext ctx)
    {
        Vector3 origin = ctx.User.transform.position + Vector3.up * rayHeight;
        Vector3 dest = ctx.TargetTile.transform.position + Vector3.up * rayHeight;
        Vector3 toDest = dest - origin;

        RaycastHit[] hits = Physics.RaycastAll(origin, toDest.normalized, toDest.magnitude, blockingMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(ctx.User.transform)) continue;
            Unit hitUnit = hit.collider.GetComponentInParent<Unit>();
            if (hitUnit == null)
            {
                Debug.Log($"[AttackCard] Attacco bloccato da {hit.collider.name}");
                return null;
            }
            return hitUnit;
        }
        return ctx.TargetUnit;
    }
}