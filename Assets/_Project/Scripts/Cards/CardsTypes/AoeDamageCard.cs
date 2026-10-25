// FILE: Assets/_Project/Scripts/Cards/AoeDamageCard.cs
// CLASSE: AoeDamageCard

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Cards/AoE Damage", fileName = "AoE_")]
public class AoeDamageCard : CardData
{
    [SerializeField] int damage = 2;
    [SerializeField] int range = 5;

    [Header("Area (centrata sulla tile target)")]
    [SerializeField] int rows = 2;
    [SerializeField] int columns = 1;

    [Header("Filtri")]
    [SerializeField] bool hitsAllies = true;
    [SerializeField] bool hitsEnemies = true;
    [SerializeField] bool hitsObjectives = true;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.User == null || ctx.TargetTile == null) return false;
        if (TileManager.Instance == null) return false;

        int dist = TileManager.Instance.GetDistance(ctx.User.Tile, ctx.TargetTile);
        return dist <= range;
    }

    public override IEnumerator Play(CardContext ctx)
    {
        if (!TrySpendCost(ctx)) yield break;   // ← valida + paga

        foreach (var t in GetAreaTiles(ctx.TargetTile))
        {
            if (t == null) continue;

            // Unità
            var u = t.Occupant;
            if (u != null && u.IsAlive)
            {
                bool isAlly = u.Faction == ctx.User.Faction;
                if ((isAlly && hitsAllies) || (!isAlly && hitsEnemies))
                {
                    // ✅ passa il source: se muore, il suo HealthManager notifica il killer
                    u.Health.TakeDamage(damage, ctx.User);
                }
            }

            // Obiettivo distruttibile
            if (hitsObjectives)
            {
                var obj = t.GetComponent<DestructibleTile>();
                if (obj != null && obj.IsAlive)
                    obj.TakeDamage(damage, ctx.User);
            }
        }

        yield break;
    }

    List<Tile> GetAreaTiles(Tile center)
    {
        var result = new List<Tile>();
        if (center == null || TileManager.Instance == null) return result;

        int x0 = center.Coord.x - (columns / 2);
        int y0 = center.Coord.y - (rows / 2);

        for (int dx = 0; dx < columns; dx++)
        {
            for (int dy = 0; dy < rows; dy++)
            {
                var coord = new Vector2Int(x0 + dx, y0 + dy);
                var t = TileManager.Instance.GetTile(coord);
                if (t != null) result.Add(t);
            }
        }
        return result;
    }
}