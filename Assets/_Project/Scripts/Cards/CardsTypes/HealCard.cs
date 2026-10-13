// FILE: Assets/_Project/Scripts/Cards/HealCard.cs
// CLASSE: HealCard

using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Heal", fileName = "Heal_")]
public class HealCard : CardData
{
    [SerializeField] int healAmount = 5;
    [SerializeField] int range = 3;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.User == null || ctx.TargetTile == null) return false;
        if (TileManager.Instance == null) return false;

        var target = ctx.TargetUnit;
        if (target == null) return false;
        if (target.Faction != ctx.User.Faction) return false;
        if (!target.IsAlive) return false;

        int dist = TileManager.Instance.GetDistance(ctx.User.Tile, ctx.TargetTile);
        return dist <= range;
    }

    public override IEnumerator Play(CardContext ctx)
    {
        if (ctx.TargetUnit == null || ctx.TargetUnit.Health == null) yield break;

        ctx.TargetUnit.Health.Heal(healAmount);
        Debug.Log($"[Heal] {ctx.TargetUnit.name} +{healAmount} HP");
        yield break;
    }
}