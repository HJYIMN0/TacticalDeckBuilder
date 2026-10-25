using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Cards/Attack", fileName = "Card_Attack")]
public class AttackCard : AbstractAttackCard
{

    public override string Description => string.IsNullOrEmpty(description) ? 
        $"Attack an unit at a maximum distance of {maxDistance} tiles for {damage} damage" : description;
    protected override int GetAttackCount(CardContext ctx)
    {
        var da = ctx.User.GetAbility<DoubleAttackAbility>();
        return da != null ? 2 : 1;
    }

    protected override int GetExtraCost(Unit user)
    {
        var da = user.GetAbility<DoubleAttackAbility>();
        return da != null ? da.extraCost : 0;
    }
}