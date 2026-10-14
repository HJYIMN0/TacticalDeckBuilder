using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bilancia attacco/movimento verso il player e verso gli objective in base a pesi
/// configurabili. Utile per nemici "intelligenti" che decidono cosa conviene.
/// </summary>
[CreateAssetMenu(menuName = "Enemy AI/Tactical", fileName = "AI_Tactical")]
public class TacticalStrategy : EnemyStrategy
{
    [Header("Pesi globali")]
    [SerializeField] float playerWeight = 1f;
    [SerializeField] float objectiveWeight = 1f;

    [Header("Bonus attacco")]
    [SerializeField] int attackPlayerBonus = 1000;
    [SerializeField] int attackObjectiveBonus = 800;
    [SerializeField] float objectivePriorityFactor = 50f;

    [Header("Peso movimento")]
    [SerializeField] float moveGainFactor = 10f;

    public override bool TryChooseNext(
     Unit self, Unit player, List<CardData> hand, Tile from,
     ISet<Tile> reservedTiles, // [MODIFICA] nuovo parametro
     out EnemyIntent choice)
    {
        choice = default;
        var objectives = FindPrioritizedObjectives(self.Faction);

        int distToPlayer = player != null
            ? TileManager.Instance.GetDistance(from, player.Tile)
            : int.MaxValue;

        CardData bestCard = null; Tile bestTile = null;
        float bestScore = float.NegativeInfinity;

        foreach (var card in hand)
        {
            if (card is AttackCard attack)
            {
                // --- Attacco al player ---
                if (player != null && distToPlayer <= attack.maxDistance)
                {
                    float s = attackPlayerBonus * playerWeight + attack.damage;
                    if (s > bestScore) { bestScore = s; bestCard = attack; bestTile = player.Tile; }
                }
                // --- Attacco a un objective ---
                foreach (var o in objectives)
                {
                    int d = TileManager.Instance.GetDistance(from, o.Tile);
                    if (d > attack.maxDistance) continue;
                    float s = attackObjectiveBonus * objectiveWeight
                            + o.AiPriority * objectivePriorityFactor;
                    if (s > bestScore) { bestScore = s; bestCard = attack; bestTile = o.Tile; }
                }
            }
            else if (card is MovementCard move)
            {
                foreach (Tile t in TileManager.Instance.GetReachableTiles(from, move.maxDistance, self))
                {
                    if (t == from) continue;
                    if (reservedTiles.Contains(t)) continue; // [MODIFICA] tile già prenotata da un altro nemico (vale per entrambi i blocchi sotto)

                    // --- Guadagno di avvicinamento al player ---
                    if (player != null)
                    {
                        int newD = TileManager.Instance.GetDistance(t, player.Tile);
                        if (newD < distToPlayer)
                        {
                            float gain = (distToPlayer - newD) * moveGainFactor * playerWeight;
                            if (gain > bestScore) { bestScore = gain; bestCard = move; bestTile = t; }
                        }
                    }

                    // --- Guadagno di avvicinamento agli objective ---
                    foreach (var o in objectives)
                    {
                        int curD = TileManager.Instance.GetDistance(from, o.Tile);
                        int newD = TileManager.Instance.GetDistance(t, o.Tile);
                        if (newD >= curD) continue;
                        float gain = (curD - newD) * moveGainFactor * objectiveWeight
                                   * (1f + o.AiPriority * 0.1f);
                        if (gain > bestScore) { bestScore = gain; bestCard = move; bestTile = t; }
                    }
                }
            }
        }

        if (bestCard == null) return false;
        choice = new EnemyIntent { card = bestCard, target = bestTile };
        return true;
    }
}