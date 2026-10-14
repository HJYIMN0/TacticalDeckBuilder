using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Enemy AI/Player Hunter", fileName = "AI_PlayerHunter")]
public class PlayerHunterStrategy : EnemyStrategy
{
    [Tooltip("Bonus quando può colpire il player (rende l'attacco prioritario al movimento).")]
    [SerializeField] int attackBonus = 1000;

    public override bool TryChooseNext(
    Unit self, Unit player, List<CardData> hand, Tile from,
    ISet<Tile> reservedTiles, // [MODIFICA] nuovo parametro
    out EnemyIntent choice)
    {
        choice = default;
        if (player == null) return false;

        int currentDist = TileManager.Instance.GetDistance(from, player.Tile);
        CardData bestCard = null; Tile bestTile = null;
        int bestScore = int.MinValue;

        foreach (var card in hand)
        {
            if (card is AttackCard attack)
            {
                if (currentDist <= attack.maxDistance)
                {
                    int score = attackBonus + attack.damage;
                    if (score > bestScore)
                    { bestScore = score; bestCard = attack; bestTile = player.Tile; }
                }
            }
            else if (card is MovementCard move)
            {
                foreach (Tile t in TileManager.Instance.GetReachableTiles(from, move.maxDistance, self))
                {
                    if (t == from) continue;
                    if (reservedTiles.Contains(t)) continue; // [MODIFICA] tile già prenotata da un altro nemico
                    int d = TileManager.Instance.GetDistance(t, player.Tile);
                    if (d >= currentDist) continue;
                    int score = -d;
                    if (score > bestScore)
                    { bestScore = score; bestCard = move; bestTile = t; }
                }
            }
        }

        if (bestCard == null) return false;
        choice = new EnemyIntent { card = bestCard, target = bestTile };
        return true;
    }
}