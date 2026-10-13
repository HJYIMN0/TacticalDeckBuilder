using System.Collections;
using UnityEngine;

public abstract class UnitAbility : ScriptableObject
{
    [Header("Info")]
    public string abilityName;
    [TextArea] public string description;
    public Sprite icon;

    // --- Hook points (override dove serve) ---

    /// <summary>Chiamato quando l'unità uccide un'altra unità.</summary>
    public virtual void OnUnitKilled(Unit owner, Unit victim) { }

    /// <summary>Chiamato quando l'unità distrugge un obiettivo.</summary>
    public virtual void OnObjectiveDestroyed(Unit owner, DestructibleTile objective) { }

    /// <summary>Chiamato a inizio turno dell'unità.</summary>
    public virtual IEnumerator OnTurnStart(Unit owner) { yield break; }

    /// <summary>Chiamato quando l'unità gioca una carta.</summary>
    public virtual void OnCardPlayed(Unit owner, CardData card) { }

    /// <summary>Chiamato quando l'unità subisce danno letale.</summary>
    public virtual void OnDeath(Unit owner) { }
}