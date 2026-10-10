using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AreaManager : GenericSingleton<AreaManager>
{
    public override bool IsDestroyedOnLoad() => true;
    public override bool ShouldDetatchFromParent() => true;

    readonly List<ConquerableTile> tiles = new();

    public void Register(ConquerableTile t)
    {
        if (t != null && !tiles.Contains(t))
            tiles.Add(t);
    }

    public void Unregister(ConquerableTile t)
    {
        tiles.Remove(t);
    }

    public void NotifyTileChanged(ConquerableTile t)
    {
        // Punto di aggancio per UI / effetti / ricalcolo bonus immediato
        Debug.Log($"[AreaManager] {t.name} → {t.Owner} (area: {t.Area?.areaName})");
    }

    public IEnumerable<ConquerableTile> TilesOf(AreaDefinition area) =>
        tiles.Where(t => t != null && t.Area == area);

    public IEnumerable<AreaDefinition> AllAreas() =>
        tiles.Where(t => t != null && t.Area != null)
             .Select(t => t.Area)
             .Distinct();

    /// <summary>
    /// Ritorna il faction che controlla l'area, o Neutral se non è uniforme.
    /// Se requireFullControl è true (default), serve che TUTTE le tile siano
    /// dello stesso faction non-neutrale.
    /// </summary>
    public Faction GetController(AreaDefinition area)
    {
        Faction first = Faction.Neutral;
        bool hasAny = false;

        foreach (var t in TilesOf(area))
        {
            hasAny = true;
            if (t.Owner == Faction.Neutral) return Faction.Neutral;
            if (first == Faction.Neutral) first = t.Owner;
            else if (first != t.Owner) return Faction.Neutral;
        }

        return hasAny ? first : Faction.Neutral;
    }

    /// <summary>
    /// Distribuisce i bonus a inizio turno di un faction.
    /// Chiamato dal PlayerController dopo ResetForTurn.
    /// </summary>
    public void ApplyBonusesFor(Faction faction, GlobalEnergyManager energy)
    {
        foreach (var area in AllAreas())
        {
            if (GetController(area) != faction) continue;

            if (area.bonusEnergy > 0 && energy != null)
                energy.Add(area.bonusEnergy);

            foreach (var card in area.bonusCards)
            {
                // se vuoi aggiungerla a TUTTE le unità del faction:
                // foreach (var u in FindObjectsByType<Unit>(...).Where(u => u.Faction == faction))
                //     u.Deck.AddBonusCards(new[] { card });
            }

            Debug.Log($"[AreaManager] {faction} controlla '{area.areaName}': +{area.bonusEnergy} energia, {area.bonusCards.Count} carte bonus");
            // TODO: iniettare area.bonusCards nel deck del faction (vedi sotto)
        }
    }
}