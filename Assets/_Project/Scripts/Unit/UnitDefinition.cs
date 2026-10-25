using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Units/Unit Definition", fileName = "Unit_")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identità")]
    public Faction faction;
    public int initiative = 5;

    [Header("Vita")]
    public int maxHp = 10;
    public int maxShield = 0;
    public bool isShieldRenewable = false;

    [Header("Abilità")]
    public List<UnitAbility> abilities = new();
}