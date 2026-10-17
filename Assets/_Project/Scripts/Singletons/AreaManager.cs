using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AreaManager : GenericSingleton<AreaManager>
{
    public override bool IsDestroyedOnLoad() => true;
    public override bool ShouldDetatchFromParent() => true;

    readonly List<ConquerableTile> tiles = new();

    // Traccia l'ultimo owner noto per ogni area, per rilevare le transizioni.
    readonly Dictionary<AreaDefinition, Faction> lastOwner = new();

    // -------------------------------------------------------------------------
    // Registrazione
    // -------------------------------------------------------------------------

    public void Register(ConquerableTile t)
    {
        if (t == null || tiles.Contains(t)) return;
        tiles.Add(t);
        if (t.Area != null) RecountArea(t.Area, null);
    }

    public void Unregister(ConquerableTile t)
    {
        if (t == null) return;
        var area = t.Area;
        tiles.Remove(t);
        if (area != null) RecountArea(area, null);
    }

    // -------------------------------------------------------------------------
    // Query
    // -------------------------------------------------------------------------

    public IEnumerable<ConquerableTile> TilesOf(AreaDefinition area) =>
        tiles.Where(t => t != null && t.Area == area);

    public IEnumerable<AreaDefinition> AllAreas() =>
        tiles.Where(t => t != null && t.Area != null)
             .Select(t => t.Area)
             .Distinct();

    // -------------------------------------------------------------------------
    // Conquista
    // -------------------------------------------------------------------------

    /// <summary>
    /// Conta le unità vive per fazione nell'area e assegna l'ownership
    /// alla fazione dominante. `trigger` = unità che ha causato il riconto
    /// (rilevante per il bonus carte quando isCardForAllFaction = false).
    /// </summary>
    public void RecountArea(AreaDefinition area, Unit trigger = null)
    {
        if (area == null) return;

        var counts = new Dictionary<Faction, int>();
        foreach (var t in TilesOf(area))
        {
            var tile = t.Tile;
            if (tile == null) continue;
            var occ = tile.Occupant;
            if (occ == null || !occ.IsAlive) continue;

            counts.TryGetValue(occ.Faction, out int c);
            counts[occ.Faction] = c + 1;
        }

        Faction winner = ComputeDominant(counts);
        ApplyOwnership(area, winner, trigger);
    }

    public void RecountAll()
    {
        foreach (var area in AllAreas())
            RecountArea(area, null);
    }

    /// <summary>
    /// Rivaluta le aree che contengono la tile indicata.
    /// `trigger` = unità che ha causato il riconto (es. chi si è mosso).
    /// </summary>
    public void RecountAreasTouching(Tile t, Unit trigger = null)
    {
        if (t == null) return;

        var areas = new HashSet<AreaDefinition>();
        foreach (var ct in tiles)
            if (ct != null && ct.Tile == t && ct.Area != null)
                areas.Add(ct.Area);

        foreach (var a in areas)
            RecountArea(a, trigger);
    }

    // -------------------------------------------------------------------------
    // Ownership + bonus cards
    // -------------------------------------------------------------------------

    static Faction ComputeDominant(Dictionary<Faction, int> counts)
    {
        Faction winner = Faction.Neutral;
        int best = 0;
        bool tie = false;

        foreach (var kv in counts)
        {
            if (kv.Value > best) { best = kv.Value; winner = kv.Key; tie = false; }
            else if (kv.Value == best && kv.Key != winner) tie = true;
        }

        return tie ? Faction.Neutral : winner;
    }

    void ApplyOwnership(AreaDefinition area, Faction newOwner, Unit trigger)
    {
        lastOwner.TryGetValue(area, out Faction previous);
        bool changed = previous != newOwner;

        if (changed)
        {
            foreach (var t in TilesOf(area))
                if (t.Owner != newOwner) t.SetOwner(newOwner);

            Debug.Log($"[AreaManager] area '{area.areaName}' ora controllata da {newOwner} (era {previous})");
        }

        lastOwner[area] = newOwner;

        // Bonus carte SOLO su transizione verso fazione non-neutrale
        if (changed && newOwner != Faction.Neutral)
            GrantBonusCards(area, newOwner, trigger);
    }

    void GrantBonusCards(AreaDefinition area, Faction owner, Unit trigger)
    {
        if (area.bonusCards == null || area.bonusCards.Count == 0) return;

        if (area.isCardForAllFaction)
        {
            var all = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);
            int given = 0;
            foreach (var u in all)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.Faction != owner) continue;

                u.AddCardsToDeck(area.bonusCards);
                given++;
                Debug.Log($"[AreaManager] +{area.bonusCards.Count} carte a {u.name} (tutta fazione {owner})");
            }
            Debug.Log($"[AreaManager] area '{area.areaName}': bonus distribuito a {given} unità");
        }
        else
        {
            if (trigger == null)
            {
                Debug.LogWarning($"[AreaManager] area '{area.areaName}' conquistata da {owner} senza trigger: " +
                                 $"bonus carte non assegnato (isCardForAllFaction = false)");
                return;
            }
            if (trigger.Faction != owner)
            {
                Debug.LogWarning($"[AreaManager] trigger '{trigger.name}' ({trigger.Faction}) ≠ owner ({owner}): " +
                                 $"bonus non assegnato");
                return;
            }

            trigger.AddCardsToDeck(area.bonusCards);
            Debug.Log($"[AreaManager] +{area.bonusCards.Count} carte a {trigger.name} (conquistatore)");
        }
    }

    // -------------------------------------------------------------------------
    // Query bonus energia (invariato)
    // -------------------------------------------------------------------------

    public Faction GetController(AreaDefinition area)
    {
        var first = TilesOf(area).FirstOrDefault();
        return first != null ? first.Owner : Faction.Neutral;
    }

    public void ApplyBonusesFor(Faction faction, IEnergyPool energy)
    {
        foreach (var area in AllAreas())
        {
            if (GetController(area) != faction) continue;
            if (area.bonusEnergy > 0 && energy != null)
                energy.Add(area.bonusEnergy);
            Debug.Log($"[AreaManager] {faction} controlla '{area.areaName}': +{area.bonusEnergy} energia");
        }
    }
}