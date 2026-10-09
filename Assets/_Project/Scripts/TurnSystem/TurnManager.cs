using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : GenericSingleton<TurnManager>
{
    public override bool IsDestroyedOnLoad() => true;
    public override bool ShouldDetatchFromParent() => true;

    public event System.Action EndTurnRequested;

    readonly List<Unit> units = new List<Unit>();
    public IReadOnlyList<Unit> Units => units;
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
                if (u == null) { Debug.Log("[TurnManager] Unità distrutta, salto"); continue; }
                if (!u.IsAlive) { Debug.Log($"[TurnManager] {u.name} è morta, salto"); continue; }
                if (u.Controller == null) { Debug.LogWarning($"[TurnManager] {u.name} senza Controller"); continue; }

                if (!handledFactions.Add(u.Faction))
                    continue; // questa fazione ha già agito in questo round

                Debug.Log($"[TurnManager] >>> Turno di fazione {u.Faction} (guida: {u.name})");
                yield return u.Controller.TakeTurn(u);
                Debug.Log($"[TurnManager] <<< Fine turno fazione {u.Faction}");
            }

            yield return null;
        }
    }
}