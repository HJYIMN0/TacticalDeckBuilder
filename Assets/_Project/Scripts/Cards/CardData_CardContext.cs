
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
    public string description;
    public virtual string Description => description;

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
    public virtual bool CanBeExecutedBy(Unit unit)
        => unit != null && unit.IsAlive;

    public abstract bool IsValidTarget(CardContext ctx);
    public abstract IEnumerator Play(CardContext ctx);

    /// <summary>
    /// Verifica se l'unità può sostenere il costo della carta.
    /// Le unità nemiche non utilizzano una riserva di energia.
    /// </summary>
    public virtual bool CanAfford(CardContext ctx)
    {
        // MODIFICA: i nemici non hanno un pool Energy da cui pagare.
        if (ctx.User != null && ctx.User.Faction == Faction.Enemy)
            return true;

        // Il comportamento originale resta invariato per il Player
        // e per le altre unità.
        return ctx.Energy != null && ctx.Energy.CanSpend(cost);
    }

    /// <summary>Validazione completa: energia + target.</summary>
    public bool CanPlay(CardContext ctx)
        => CanAfford(ctx) && IsValidTarget(ctx);

    /// <summary>
    /// Valida e, se tutto ok, spende il costo.
    /// Ritorna true se la carta può procedere con l'effetto.
    /// Da chiamare come prima riga di Play():
    ///     if (!TrySpendCost(ctx)) yield break;
    ///
    /// Override se una carta ha bisogno di logica di costo
    /// diversa (costo variabile, sconto, rimborso, ecc.).
    /// </summary>
    protected virtual bool TrySpendCost(CardContext ctx)
    {
        // MODIFICA: per i nemici controlla il bersaglio,
        // ma non tenta di spendere energia da un pool inesistente.
        if (ctx.User != null && ctx.User.Faction == Faction.Enemy)
            return IsValidTarget(ctx);

        // Comportamento originale per il Player e le altre unità.
        if (!CanPlay(ctx)) return false;

        return ctx.Energy.Spend(cost);
    }
}

public struct CardContext
{
    /// <summary>
    /// L'unità che esegue la carta.
    /// Player ExecutorAndTile → esecutore scelto dall'utente
    /// Player UnitOnly       → unità scelta (esecutore = target)
    /// Player TileOnly       → null
    /// Enemy                 → unità che esegue l'intent
    /// </summary>
    public Unit User;

    public Tile TargetTile;

    /// <summary>
    /// Riserva di energia.
    /// Player → FactionEnergyPool condivisa.
    /// Enemy → non necessaria per l'esecuzione delle carte nemiche.
    /// </summary>
    public IEnergyPool Energy;

    public Unit TargetUnit => TargetTile != null ? TargetTile.Occupant : null;
}
