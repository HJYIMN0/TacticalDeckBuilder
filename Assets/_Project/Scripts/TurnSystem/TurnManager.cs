using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : GenericSingleton<TurnManager>
{
    public override bool IsDestroyedOnLoad() => true;
    public override bool ShouldDetatchFromParent() => true;

    public event System.Action EndTurnRequested;

    readonly List<Unit> units = new List<Unit>();
    public IReadOnlyList<Unit> Units => units;
    readonly HashSet<Tile> plannedTiles = new HashSet<Tile>();     // registro prenotazioni, posseduto da TurnManager (non singleton a sé)
    public ISet<Tile> PlannedTiles => plannedTiles;
    bool running;
    int round;

    void Start()
    {
        var btn = FindAnyObjectByType<EndTurnButton>();
        if (btn != null)
        {
            btn.Clicked += HandleEndTurnClick;
            Debug.Log("[TurnManager] Agganciato EndTurnButton");
        }
        else
        {
            Debug.LogWarning("[TurnManager] Nessun EndTurnButton in scena");
        }

        var found = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);
        units.AddRange(found);
        units.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));

        // leggere Tile forza l'init del GridMover (e quindi Tile.SetOccupant) di TUTTE le unità
        // prima di pianificare: senza, chi pianifica per primo vede libere le tile degli altri nemici
        foreach (var u in units)
            _ = u.Tile;

        // Pre-dichiara le intenzioni iniziali dei nemici,
        // così al Round 1 hanno già qualcosa in coda da eseguire.
        plannedTiles.Clear();
        foreach (var u in units)
        {
            if (u == null || !u.IsAlive) continue;
            if (u.Controller is EnemyAiController ai)
                ai.PreDeclareInitialIntent(u);
        }

        Debug.Log($"[TurnManager] {units.Count} unità registrate:");
        for (int i = 0; i < units.Count; i++)
            Debug.Log($"  [{i}] {units[i].name} | faction {units[i].Faction} | init {units[i].Initiative}");

        if (!running) StartCoroutine(TurnLoop());
    }

    void HandleEndTurnClick()
    {
        Debug.Log("[TurnManager] End turn richiesto dall'utente");
        EndTurnRequested?.Invoke();
    }

    public void Register(Unit u)
    {
        if (!units.Contains(u)) { units.Add(u); Debug.Log($"[TurnManager] Registrata {u.name}"); }
    }

    public void Unregister(Unit u)
    {
        if (units.Remove(u)) Debug.Log($"[TurnManager] Deregistrata {u.name}");
    }

    IEnumerator TurnLoop()
    {
        running = true;
        while (true)
        {
            round++;
            Debug.Log($"========== ROUND {round} ==========");

            var handledFactions = new HashSet<Faction>();

            foreach (Unit u in units.ToArray())
            {
                if (u == null || !u.IsAlive || u.Controller == null) continue;
                if (!handledFactions.Add(u.Faction)) continue;

                plannedTiles.Clear();

                var factionUnits = new List<Unit>();
                foreach (var x in units)
                    if (x != null && x.IsAlive && x.Faction == u.Faction)
                        factionUnits.Add(x);

                // PLAYER: UN SOLO turno di fazione
                if (u.Faction == Faction.Player)
                {
                    var pc = factionUnits
                        .Select(fu => fu.Controller)
                        .OfType<PlayerController>()
                        .FirstOrDefault();

                    if (pc != null)
                    {
                        Debug.Log($"[TurnManager] >>> Turno fazione PLAYER ({factionUnits.Count} unità)");
                        yield return pc.TakeFactionTurn(factionUnits);
                        Debug.Log("[TurnManager] <<< Fine turno fazione PLAYER");
                    }
                    continue;
                }

                // ENEMY: comportamento invariato (un turno per unità)
                foreach (var fu in factionUnits)
                {
                    if (fu == null || !fu.IsAlive || fu.Controller == null) continue;
                    Debug.Log($"[TurnManager] >>> Turno di {fu.name} ({fu.Faction})");
                    yield return fu.Controller.TakeTurn(fu);
                    Debug.Log($"[TurnManager] <<< Fine turno di {fu.name}");
                }
            }

            yield return null;
        }
    }
}