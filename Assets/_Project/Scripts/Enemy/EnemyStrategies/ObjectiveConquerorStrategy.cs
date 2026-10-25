using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Enemy/AIType/Objective Conqueror", fileName = "AI_Objective")]
public class ObjectiveConquerorStrategy : EnemyStrategy
{
    [Tooltip("Bonus per attacco su objective (le carte attacco colpiscono anche DestructibleTile).")]
    [SerializeField] int attackBonus = 500;

    public override bool TryChooseNext(
     Unit self, Unit player, List<CardData> hand, Tile from,
     ISet<Tile> reservedTiles, // [MODIFICA] nuovo parametro
     out EnemyIntent choice)
    {
        choice = default;
        var objectives = FindPrioritizedObjectives(self.Faction);
        if (objectives.Count == 0) return false;

        CardData bestCard = null; Tile bestTile = null;
        int bestScore = int.MinValue;

        foreach (var card in hand)
        {
            if (card is AttackCard attack)
            {
                foreach (var o in objectives)
                {
                    int d = TileManager.Instance.GetDistance(from, o.Tile);
                    if (d > attack.maxDistance) continue;
                    int score = attackBonus + o.AiPriority * 100 + attack.damage;
                    if (score > bestScore)
                    { bestScore = score; bestCard = attack; bestTile = o.Tile; }
                }
            }
            else if (card is MovementCard move)
            {
                foreach (Tile t in TileManager.Instance.GetReachableTiles(from, move.maxDistance, self))
                {
                    if (t == from) continue;
                    if (reservedTiles.Contains(t)) continue; // [MODIFICA] tile già prenotata da un altro nemico: provo la prossima migliore
                    foreach (var o in objectives)
                    {
                        int curD = TileManager.Instance.GetDistance(from, o.Tile);
                        int newD = TileManager.Instance.GetDistance(t, o.Tile);
                        if (newD >= curD) continue;
                        // Prioritizza objective "pesanti" e vicini
                        int score = (curD - newD) * 10 + o.AiPriority * 5 - newD;
                        if (score > bestScore)
                        { bestScore = score; bestCard = move; bestTile = t; }
                    }
                }
            }
        }

        if (bestCard == null) return false;
        choice = new EnemyIntent { card = bestCard, target = bestTile };
        return true;
    }
}