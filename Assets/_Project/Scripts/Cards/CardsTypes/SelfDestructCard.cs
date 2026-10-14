using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Self Destruct", fileName = "SelfDestruct_")]
public class SelfDestructCard : CardData
{
    [SerializeField] int damage = 4;
    [SerializeField] int radius = 1;
    [SerializeField] bool hitsAllies = true;
    [SerializeField] bool hitsEnemies = true;
    [SerializeField] bool hitsObjectives = true;

    public int Radius => radius;

    public override bool IsValidTarget(CardContext ctx)
    {
        return ctx.User != null && ctx.User.Tile != null;
    }

    public override IEnumerator Play(CardContext ctx)
    {
        var center = ctx.User.Tile;
        if (center == null) yield break;

        foreach (var t in GetAreaTiles(center, radius))
        {
            if (t == null) continue;
            if (t == center) continue; // il centro è l'utente che si sacrifica

            var u = t.Occupant;
            if (u != null && u.IsAlive)
            {
                bool isAlly = u.Faction == ctx.User.Faction;
                if ((isAlly && hitsAllies) || (!isAlly && hitsEnemies))
                    u.Health.TakeDamage(damage, ctx.User);
            }

            if (hitsObjectives)
            {
                var obj = t.GetComponent<DestructibleTile>();
                if (obj != null && obj.IsAlive)
                    obj.TakeDamage(damage, ctx.User);
            }
        }

        // Autodistruzione: danno enorme alla fonte stessa
        if (ctx.User.IsAlive)
            ctx.User.Health.TakeDamage(9999, ctx.User);

        yield break;
    }

    List<Tile> GetAreaTiles(Tile center, int r)
    {
        var result = new List<Tile>();
        if (center == null || TileManager.Instance == null) return result;
        int cx = center.Coord.x, cy = center.Coord.y;
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                var t = TileManager.Instance.GetTile(new Vector2Int(cx + dx, cy + dy));
                if (t != null) result.Add(t);
            }
        return result;
    }
}