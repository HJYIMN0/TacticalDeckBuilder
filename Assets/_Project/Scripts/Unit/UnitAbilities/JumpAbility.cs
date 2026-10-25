using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Units/UnitsAbilities/Jump", fileName = "Ability_Jump")]
public class JumpAbility : UnitAbility
{
    [Tooltip("Moltiplicatore del range delle carte movimento quando si salta. Es. 0.5 = metà range.")]
    [Range(0.1f, 1f)]
    public float rangeMultiplier = 0.5f;
}
