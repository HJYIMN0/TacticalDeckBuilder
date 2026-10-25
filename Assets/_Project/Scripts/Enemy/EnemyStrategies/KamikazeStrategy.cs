using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Enemy/AIType/Kamikaze", fileName = "AI_Kamikaze")]
public class KamikazeStrategy : EnemyStrategy
{
    public override bool TryChooseNext(
    Unit self, Unit player, List<CardData> hand, Tile from,
    ISet<Tile> reservedTiles, // [MODIFICA] nuovo parametro
    out EnemyIntent choice)
    {
        choice = default;

        // 1) Se ho self-destruct e un nemico è nel raggio → detono subito
        foreach (var card in hand)
        {
            if (card is SelfDestructCard sd && AnyEnemyInRadius(self, from, sd.Radius))
            {
                choice = new EnemyIntent { card = sd, target = from };
                return true;
            }
        }

        // 2) Altrimenti mi avvicino il più possibile al nemico più vicino
        Unit target = FindNearestEnemy(self, from);
        if (target == null) return false;

        int currentDist = TileManager.Instance.GetDistance(from, target.Tile);
        CardData bestCard = null; Tile bestTile = null;
        int bestDist = currentDist;

        foreach (var card in hand)
        {
            if (card is MovementCard move)
            {
                foreach (Tile t in TileManager.Instance.GetReachableTiles(from, move.maxDistance, self))
                {
                    if (t == from) continue;
                    if (reservedTiles.Contains(t)) continue; // [MODIFICA] tile già prenotata da un altro nemico
                    int d = TileManager.Instance.GetDistance(t, target.Tile);
                    if (d < bestDist) { bestDist = d; bestCard = move; bestTile = t; }
                }
            }
        }

        if (bestCard == null) return false;
        choice = new EnemyIntent { card = bestCard, target = bestTile };
        return true;
    }

    bool AnyEnemyInRadius(Unit self, Tile from, int radius)
    {
        if (!TurnManager.HasInstance) return false;
        foreach (var u in TurnManager.Instance.Units)
        {
            if (u == null || !u.IsAlive) continue;
            if (u.Faction == self.Faction) continue;
            if (TileManager.Instance.GetDistance(from, u.Tile) <= radius) return true;
        }
        return false;
    }
}