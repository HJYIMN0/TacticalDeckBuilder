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

    [Header("Settings Animazioni")]
    [Min(0.1f)] public float moveSpeed = 4f;       // unità di mondo al secondo, per singolo step
    [Min(0f)] public float stepDelay = 0.08f;      // attesa tra una tile e l'altra
    [Min(0f)] public float hopHeight = 0.15f;      // 0 = scivola senza saltello
}