using System.Collections;
using UnityEngine;

public enum CardTargeting
{
    /// <summary>Attack, Movement, AoE, SelfDestruct. Carta → Unità → Tile.</summary>
    ExecutorAndTile,
    /// <summary>Heal, self-buff. Carta → Unità (unità = esecutore = target).</summary>
    UnitOnly,
    /// <summary>Tile transform. Carta → Tile (nessun esecutore).</summary>
    TileOnly
}

public abstract class CardData : ScriptableObject
{
    public string cardName;
    public int cost = 1;
    public Sprite artwork;

    [Tooltip("Se true, una volta usata o scartata non si rimescola mai più")]
    public bool isUnique = false;

    [Header("Targeting")]
    [Tooltip("Determina il flusso di selezione richiesto.")]
    [SerializeField] CardTargeting targeting = CardTargeting.ExecutorAndTile;
    public virtual CardTargeting TargetMode => targeting;

    /// <summary>
    /// Se l'unità può eseguire questa carta.
    /// Usato in ExecutorAndTile (per l'esecutore) e UnitOnly (per il target).
    /// </summary>
    public virtual bool CanBeExecutedBy(Unit unit) => unit != null && unit.IsAlive;

    public abstract bool IsValidTarget(CardContext ctx);
    public abstract IEnumerator Play(CardContext ctx);
}

public struct CardContext
{
    /// <summary>
    /// L'unità che ESEGUE la carta.
    /// • Player ExecutorAndTile → esecutore scelto dall'utente
    /// • Player UnitOnly       → unità scelta (esecutore = target)
    /// • Player TileOnly       → null
    /// • Enemy                 → unità che esegue l'intent
    /// </summary>
    public Unit User;

    public Tile TargetTile;

    /// <summary>
    /// Riserva di energia (interfaccia comune: fazione o unità).
    /// Player  → FactionEnergyPool condivisa.
    /// Enemy   → UnitEnergyPool per unità (se presente).
    /// </summary>
    public IEnergyPool Energy;

    public Unit TargetUnit => TargetTile != null ? TargetTile.Occupant : null;
}