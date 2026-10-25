using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Units/UnitsAbilities/Trample", fileName = "Ability_Trample")]
public class TrampleAbility : UnitAbility
{
    [Tooltip("Danno inflitto ai nemici attraversati durante il movimento.")]
    public int damage = 1;
}