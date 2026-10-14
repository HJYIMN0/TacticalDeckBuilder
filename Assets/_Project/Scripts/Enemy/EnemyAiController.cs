using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAiController : AbstractUnitController
{
    [SerializeField] int maxPlaysPerTurn = 3;
    [SerializeField] float thinkDelay = 0.4f;
    [SerializeField] float playDelay = 0.3f;
    [SerializeField] bool shouldDeclareAgain = true;

    [Header("Comportamento")]
    [Tooltip("Strategy che decide la pianificazione. Se null, l'enemy non fa nulla.")]
    [SerializeField] EnemyStrategy strategy;

    readonly Queue<EnemyIntent> pendingIntents = new Queue<EnemyIntent>();

    public IReadOnlyCollection<EnemyIntent> PendingIntents => pendingIntents;
    public bool HasPendingIntent => pendingIntents.Count > 0;
    public bool ShouldDeclareAgain => shouldDeclareAgain;

    public event System.Action OnIntentChanged;

    void NotifyIntentChanged() => OnIntentChanged?.Invoke();

    void Awake()
    {
        if (strategy == null)
            Debug.LogError($"[EnemyAiController] {name}: nessuna strategy assegnata! " +
                           "Il nemico non farà nulla. Trascina un asset EnemyStrategy nell'Inspector.", this);
    }

    /// <summary>
    /// Pre-dichiara le intenzioni iniziali. Chiamato da TurnManager prima che il TurnLoop parta.
    /// Pesca, pianifica, scarta: così al Round 1 l'enemy ha già qualcosa in coda da eseguire.
    /// </summary>
    public void PreDeclareInitialIntent(Unit unit)
    {
        if (strategy == null) return;
        if (pendingIntents.Count > 0) return; // già qualcosa in coda: non toccare

        Unit player = FindPlayer();
        if (player == null) return;

        Debug.Log($"[EnemyAiController] {name} unit.Tile={(unit.Tile != null ? unit.Tile.Coord.ToString() : "NULL")} | " +
                  $"player={player.name} player.Tile={(player.Tile != null ? player.Tile.Coord.ToString() : "NULL")}");

        unit.Deck.Draw(unit.DrawPerTurn);
        DeclareIntentions(unit, player);
        unit.Deck.DiscardHand();

        Debug.Log($"[Enemy] Pre-dichiarate {pendingIntents.Count} intenzioni iniziali");
    }

    public override IEnumerator TakeTurn(Unit unit)
    {
        Debug.Log($"[Enemy] === Turno di {unit.name} | pending={pendingIntents.Count} ===");

        Unit player = FindPlayer();
        if (player == null)
        {
            Debug.Log("[Enemy] Nessun player vivo, turno saltato");
            pendingIntents.Clear();
            NotifyIntentChanged();
            yield break;
        }

        unit.Deck.Draw(unit.DrawPerTurn);

        bool hadPending = pendingIntents.Count > 0;

        // FASE ESECUZIONE
        if (hadPending)
        {
            Debug.Log($"[Enemy] Eseguo {pendingIntents.Count} intenzioni dichiarate");
            while (pendingIntents.Count > 0)
            {
                EnemyIntent intent = pendingIntents.Dequeue();

                if (TryBuildContext(unit, intent, out CardContext ctx))
                {
                    Debug.Log($"[Enemy] Eseguo {intent.card.cardName} -> {ctx.TargetTile.Coord}"); // [MODIFICA] tile effettiva (può differire dall'intent)
                    yield return intent.card.Play(ctx);
                    yield return new WaitForSeconds(playDelay);
                }

                NotifyIntentChanged();
            }
        }

        // FASE DICHIARAZIONE
        bool declareNow = !hadPending || shouldDeclareAgain;
        if (declareNow)
        {
            Debug.Log("[Enemy] Dichiaro nuove intenzioni");
            yield return new WaitForSeconds(thinkDelay);
            DeclareIntentions(unit, player);
        }

        unit.Deck.DiscardHand();
    }

    void DeclareIntentions(Unit unit, Unit player)
    {
        pendingIntents.Clear();
        var planned = PlanActions(unit, player);
        foreach (var intent in planned) pendingIntents.Enqueue(intent);

        Debug.Log($"[Enemy] Dichiaro {pendingIntents.Count} intenzioni");
        foreach (var i in pendingIntents)
            Debug.Log($"  - {i.card.cardName} -> {i.target.Coord}");

        NotifyIntentChanged();
    }

    List<EnemyIntent> PlanActions(Unit unit, Unit player)
    {
        var result = new List<EnemyIntent>();
        if (strategy == null) return result;

        // set condiviso fra tutti i nemici nello stesso ciclo di pianificazione (resettato da TurnManager.TurnLoop)
        ISet<Tile> reserved = TurnManager.Instance.PlannedTiles;

        var simulatedHand = new List<CardData>(unit.Deck.Hand);
        Tile simulatedPos = unit.Tile;

        for (int plays = 0; plays < maxPlaysPerTurn; plays++)
        {
            if (!strategy.TryChooseNext(unit, player, simulatedHand, simulatedPos, reserved, out var choice))
                break;

            result.Add(choice);
            simulatedHand.Remove(choice.card);

            // Solo il movimento sposta la posizione simulata
            if (choice.card is MovementCard)
                simulatedPos = choice.target;
        }

        // prenoto solo la tile finale: è quella su cui il nemico resta quando agisce il successivo
        if (simulatedPos != unit.Tile)
        {
            reserved.Add(simulatedPos);
            Debug.Log($"[EnemyAiController] {name} prenota {simulatedPos.Coord}");
        }

        // diagnosi piano vuoto: strategy usata e mano di partenza
        if (result.Count == 0)
            Debug.Log($"[EnemyAiController] {name}: piano vuoto | strategy={strategy.name} | da {unit.Tile.Coord} " +
                      $"| player a {player.Tile.Coord} | mano=[{string.Join(", ", unit.Deck.Hand)}]");

        return result;
    }

    Unit FindPlayer()
    {
        if (!TurnManager.HasInstance) return null;
        foreach (Unit u in TurnManager.Instance.Units)
        {
            if (u == null) continue;
            if (!u.IsAlive) continue;
            if (u.Faction == Faction.Player) return u;
        }
        return null;
    }

    bool TryBuildContext(Unit unit, EnemyIntent intent, out CardContext ctx)
    {
        ctx = default;

        if (intent.card == null || intent.target == null)
        {
            Debug.LogWarning($"[EnemyAiController] {name}: intent malformato, salto");
            return false;
        }

        Unit occupant = intent.target.Occupant;
        bool allyOnTarget = occupant != null && occupant.Faction == unit.Faction;

        ctx = new CardContext { User = unit, TargetTile = intent.target };

        if (!allyOnTarget && intent.card.IsValidTarget(ctx))
            return true;

        // [MODIFICA] movimento non più eseguibile (tile occupata, percorso bloccato, fuori range):
        // vado sulla tile raggiungibile più vicina al target invece di perdere il turno
        if (intent.card is MovementCard move && TryAdaptMovement(unit, move, intent.target, out Tile alt))
        {
            var altCtx = new CardContext { User = unit, TargetTile = alt };
            if (intent.card.IsValidTarget(altCtx))
            {
                Debug.Log($"[EnemyAiController] {name} ADATTA movimento {intent.target.Coord} -> {alt.Coord}");
                ctx = altCtx;
                return true;
            }
        }

        Debug.Log($"[EnemyAiController] {name} SKIP {intent.card.cardName} -> {intent.target.Coord}: non eseguibile " +
                  $"| da {unit.Tile.Coord} | dist={TileManager.Instance.GetDistance(unit.Tile, intent.target)} " +
                  $"| occupante={(occupant != null ? occupant.name : "nessuno")}");
        return false;
    }

    // Tile raggiungibile con questa carta che avvicina di più il nemico a `wanted`. Evita le tile già prenotate
    // da altri nemici in questo turno di fazione. Ritorna false se nessuna tile migliora la distanza attuale.
    bool TryAdaptMovement(Unit unit, MovementCard move, Tile wanted, out Tile best)
    {
        best = null;
        ISet<Tile> reserved = TurnManager.Instance.PlannedTiles;
        int bestDist = TileManager.Instance.GetDistance(unit.Tile, wanted);

        foreach (Tile t in TileManager.Instance.GetReachableTiles(unit.Tile, move.maxDistance, unit))
        {
            if (t == unit.Tile || reserved.Contains(t)) continue;
            int d = TileManager.Instance.GetDistance(t, wanted);
            if (d < bestDist) { bestDist = d; best = t; }
        }

        return best != null;
    }


}