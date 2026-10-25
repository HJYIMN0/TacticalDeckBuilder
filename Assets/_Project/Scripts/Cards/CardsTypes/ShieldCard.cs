using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Cards/Shield", fileName = "Card_Shield")]
public class ShieldCard : CardData
{
    public int shieldAmount = 3;

    public override CardTargeting TargetMode => CardTargeting.UnitOnly;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.User == null || ctx.TargetUnit == null) return false;
        if (ctx.TargetUnit.Faction != ctx.User.Faction) return false;
        return ctx.TargetUnit.IsAlive;
    }

    public override IEnumerator Play(CardContext ctx)
    {
        if (!TrySpendCost(ctx)) yield break;
        ctx.TargetUnit.Health.AddShield(shieldAmount);
        Debug.Log($"[ShieldCard] {ctx.TargetUnit.name} +{shieldAmount} scudo");
    }
}