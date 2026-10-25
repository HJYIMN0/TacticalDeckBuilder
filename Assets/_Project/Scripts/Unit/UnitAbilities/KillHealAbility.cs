using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Units/UnitsAbilities/Kill Heal", fileName = "Ability_KillHeal")]
public class KillHealAbility : UnitAbility
{
    public int healAmount = 2;

    public override void OnUnitKilled(Unit owner, Unit victim)
    {
        if (owner == null || owner.Health == null) return;
        owner.Health.Heal(healAmount);
        Debug.Log($"[KillHeal] {owner.name} cura {healAmount} per aver ucciso {victim?.name}");
    }
}