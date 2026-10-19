// FILE: Assets/_Project/Scripts/Cards/AttackCard.cs
// CLASSE: AttackCard

using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Attack")]
public class AttackCard : CardData
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

        int dist = TileManager.Instance.GetDistance(ctx.User.Tile, ctx.TargetTile);
        return dist <= maxDistance;
    }

    public override IEnumerator Play(CardContext ctx)
    {
        if (!TrySpendCost(ctx)) yield break;   // ← valida + paga

        Unit target = ctx.TargetUnit;

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
                yield break;
            }

            target = hitUnit;
            break;
        }

        Debug.Log($"[Attack] {ctx.User.name} -> {target.name} per {damage} danni");

        // ✅ UNA SOLA RIGA: il HealthManager pensa a morire e notificare
        target.Health.TakeDamage(damage, ctx.User);

        yield break;
    }
}