using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Units/UnitsAbilities/Double Attack", fileName = "Ability_DoubleAttack")]
public class DoubleAttackAbility : UnitAbility
{
    [Tooltip("Costo extra aggiunto alle carte attacco quando giocate da questa unità. Impostato a 0 di default.")]
    public int extraCost = 0;
}