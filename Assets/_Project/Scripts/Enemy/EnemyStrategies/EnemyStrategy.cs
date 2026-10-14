using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyStrategy : ScriptableObject
{
    /// <summary>
    /// Sceglie la prossima azione data la mano simulata e la posizione simulata.
    /// Ritorna false se non c'è nulla di sensato da fare.
    /// </summary>
    public abstract bool TryChooseNext(
        Unit self, Unit player,
        List<CardData> hand, Tile from,
        ISet<Tile> reservedTiles,   // tile di movimento già prenotate da altri nemici in questo ciclo di pianificazione (sola lettura per la strategy)
        out EnemyIntent choice);

    // Helper condivisi
    protected static List<ObjectiveTile> FindPrioritizedObjectives(Faction own)
    {
        var all = Object.FindObjectsByType<ObjectiveTile>(FindObjectsInactive.Exclude);
        var result = new List<ObjectiveTile>();
        foreach (var o in all)
        {
            if (o == null || o.Tile == null) continue;
            if (o.Owner == own) continue; // già mio, non interessante
            result.Add(o);
        }
        result.Sort((a, b) => b.AiPriority.CompareTo(a.AiPriority));
        return result;
    }

    protected static Unit FindNearestEnemy(Unit self, Tile from)
    {
        if (!TurnManager.HasInstance) return null;
        Unit best = null; int bestD = int.MaxValue;
        foreach (var u in TurnManager.Instance.Units)
        {
            if (u == null || !u.IsAlive) continue;
            if (u.Faction == self.Faction) continue;
            int d = TileManager.Instance.GetDistance(from, u.Tile);
            if (d < bestD) { bestD = d; best = u; }
        }
        return best;
    }
}