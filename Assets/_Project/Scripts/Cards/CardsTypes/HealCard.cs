using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Heal", fileName = "Heal_")]
public class HealCard : CardData
{
    [SerializeField] int healAmount = 3;
    [SerializeField] Faction allowedFaction = Faction.Player;

    // Sempre UnitOnly: nessuna tile, nessun executor separato.
    public override CardTargeting TargetMode => CardTargeting.UnitOnly;

    public override bool CanBeExecutedBy(Unit unit)
    {
        if (unit == null || !unit.IsAlive) return false;
        return unit.Faction == allowedFaction;
    }

    public override bool IsValidTarget(CardContext ctx)
    {
        // In UnitOnly, ctx.User è l'unità scelta (e TargetTile è la sua tile).
        return ctx.User != null && ctx.User.IsAlive && CanBeExecutedBy(ctx.User);
    }

    public override IEnumerator Play(CardContext ctx)
    {
        var target = ctx.User;
        if (target == null || !target.IsAlive) yield break;

        Debug.Log($"[Heal] {target.name} +{healAmount}");
        target.Health.Heal(healAmount);   // richiede AbstractHealthManager.Heal(int)
        yield break;
    }
}