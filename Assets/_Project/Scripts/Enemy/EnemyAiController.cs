using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAiController : AbstractUnitController
{
    [SerializeField] int maxPlaysPerTurn = 3;
    [SerializeField] float thinkDelay = 0.4f;
    [SerializeField] float playDelay = 0.3f;
    [SerializeField] bool shouldDeclareAgain = true;

    public struct Intent
    {
        public CardData card;
        public Tile target;
    }

    readonly Queue<Intent> pendingIntents = new Queue<Intent>();

    public IReadOnlyCollection<Intent> PendingIntents => pendingIntents;
    public bool HasPendingIntent => pendingIntents.Count > 0;
    public bool ShouldDeclareAgain => shouldDeclareAgain;

    public event System.Action OnIntentChanged;

    public override IEnumerator TakeTurn(Unit unit)
    {
        Debug.Log($"[Enemy] === Turno di {unit.name} | shouldDeclareAgain={shouldDeclareAgain} | pending={pendingIntents.Count} ===");

        Unit player = FindPlayer();
        if (player == null)
        {
            Debug.Log("[Enemy] Nessun player vivo, turno saltato");
            pendingIntents.Clear();
            NotifyIntentChanged();
            yield break;
        }

        unit.Deck.Draw(unit.DrawPerTurn);
        Debug.Log($"[Enemy] Pescate {unit.Deck.Hand.Count} carte");

        bool hadPending = pendingIntents.Count > 0;

        // FASE ESECUZIONE
        if (hadPending)
        {
            Debug.Log($"[Enemy] Eseguo {pendingIntents.Count} intenzioni dichiarate");
            while (pendingIntents.Count > 0)
            {
                Intent intent = pendingIntents.Dequeue();
                NotifyIntentChanged();

                var ctx = new CardContext { User = unit, TargetTile = intent.target };
                Debug.Log($"[Enemy] Eseguo {intent.card.cardName} -> {intent.target.Coord}");
                yield return intent.card.Play(ctx);
                yield return new WaitForSeconds(playDelay);
            }
        }
        else
        {
            Debug.Log("[Enemy] Niente in coda da eseguire");
        }

        // FASE DICHIARAZIONE
        // Se non c'era nulla da eseguire, dichiaro per forza (altrimenti la coda resta vuota per sempre).
        // Se ho appena eseguito, decido in base a shouldDeclareAgain.
        bool declareNow = !hadPending || shouldDeclareAgain;

        if (declareNow)
        {
            Debug.Log("[Enemy] Dichiaro nuove intenzioni per il prossimo ciclo");
            yield return new WaitForSeconds(thinkDelay);
            DeclareIntentions(unit, player);
        }
        else
        {
            Debug.Log("[Enemy] shouldDeclareAgain=false: dichiaro al prossimo turno");
        }

        unit.Deck.DiscardHand();
        Debug.Log($"[Enemy] === Fine turno di {unit.name} ===");
    }

    void DeclareIntentions(Unit unit, Unit player)
    {
        pendingIntents.Clear();
        var planned = PlanActions(unit, player);
        foreach (var intent in planned) pendingIntents.Enqueue(intent);

        Debug.Log($"[Enemy] Dichiaro {pendingIntents.Count} intenzioni:");
        foreach (var i in pendingIntents)
            Debug.Log($"  - {i.card.cardName} -> {i.target.Coord}");

        NotifyIntentChanged();
    }

    List<Intent> PlanActions(Unit unit, Unit player)
    {
        var result = new List<Intent>();
        var simulatedHand = new List<CardData>(unit.Deck.Hand);
        Tile simulatedPos = unit.Tile;

        for (int plays = 0; plays < maxPlaysPerTurn; plays++)
        {
            var choice = ChooseNext(simulatedHand, simulatedPos, player);
            if (choice == null) break;

            result.Add(new Intent { card = choice.Value.card, target = choice.Value.tile });
            simulatedHand.Remove(choice.Value.card);
            simulatedPos = choice.Value.tile;
        }

        return result;
    }

    struct Choice { public CardData card; public Tile tile; }

    Choice? ChooseNext(List<CardData> hand, Tile from, Unit player)
    {
        CardData bestCard = null;
        Tile bestTile = null;
        int bestScore = int.MinValue;
        int currentDist = TileManager.Instance.GetDistance(from, player.Tile);

        foreach (CardData card in hand)
        {
            if (card is AttackCard attack)
            {
                if (TileManager.Instance.GetDistance(from, player.Tile) <= attack.maxDistance)
                {
                    int score = 1000 + attack.damage;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCard = attack;
                        bestTile = player.Tile;
                    }
                }
            }
            else if (card is MovementCard move)
            {
                var reachable = TileManager.Instance.GetReachableTiles(from, move.maxDistance);
                foreach (Tile t in reachable)
                {
                    if (t == from) continue;
                    int dist = TileManager.Instance.GetDistance(t, player.Tile);
                    if (dist >= currentDist) continue;

                    int score = -dist;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCard = move;
                        bestTile = t;
                    }
                }
            }
        }

        if (bestCard == null) return null;
        return new Choice { card = bestCard, tile = bestTile };
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

    void NotifyIntentChanged() => OnIntentChanged?.Invoke();

    // CLASSE: EnemyAiController
    // Aggiungi questo metodo di utility

    List<ObjectiveTile> FindPrioritizedObjectives()
    {
        var all = FindObjectsByType<ObjectiveTile>(FindObjectsInactive.Exclude);
        var result = new List<ObjectiveTile>();

        foreach (var o in all)
        {
            if (o == null || o.Tile == null) continue;
            // opzionale: salta quelli già tuoi
            if (o.Owner == GetComponent<Unit>().Faction) continue;
            result.Add(o);
        }

        // ordina per priorità decrescente
        result.Sort((a, b) => b.AiPriority.CompareTo(a.AiPriority));
        return result;
    }
}