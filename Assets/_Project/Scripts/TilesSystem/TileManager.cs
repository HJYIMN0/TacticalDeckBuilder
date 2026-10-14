using System.Collections.Generic;
using UnityEngine;

public class TileManager : GenericSingleton<TileManager>
{
    [SerializeField] float cellSize = 1f;

    [Header("Terreno")]
    [Tooltip("Dislivello massimo tra i bordi di due tile adiacenti per poter passare dall'una all'altra.")]
    [SerializeField, Min(0f)] float maxStepHeight = 0.5f;
    [Tooltip("Una tile più inclinata di così non è calpestabile.")]
    [SerializeField, Range(0f, 89f)] float maxWalkableSlope = 45f;

    public override bool IsDestroyedOnLoad() => true;
    public override bool ShouldDetatchFromParent() => true;

    readonly Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

    public Vector2Int WorldToCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.z / cellSize));
    }

    public void Register(Tile tile)
    {
        Vector2Int coord = tile.Coord;

        if (tiles.TryGetValue(coord, out Tile existing) && existing != tile)
            Debug.LogWarning($"[TileManager] {tile.name} e {existing.name} stessa coord {coord}: sovrascrivo");

        tiles[coord] = tile;
    }

    public Tile GetTile(Vector2Int coord)
    {
        if (tiles.TryGetValue(coord, out Tile tile)) return tile;
        Debug.Log($"[TileManager] Nessuna tile in {coord}. Registrate: {string.Join(" ", tiles.Keys)}");
        return null;
    }

    public bool TryGetTile(Vector2Int coord, out Tile tile)
    {
        return tiles.TryGetValue(coord, out tile);
    }

    public int GetDistance(Tile a, Tile b)
    {
        return Mathf.Abs(a.Coord.x - b.Coord.x) + Mathf.Abs(a.Coord.y - b.Coord.y);
    }

    // BFS su 4 direzioni. Non richiede che start sia walkable (sopra c'è l'unità stessa).
    // Il goal è raggiungibile solo se è una tile walkable.
    // `mover`: se passato, ignora le tile occupate da alleati del mover (ma non il goal).
    public bool TryGetPath(Tile start, Tile goal, int maxDistance, out List<Tile> path, Unit mover = null)
    {
        path = null;
        if (start == null || goal == null) return false;

        if (start == goal)
        {
            path = new List<Tile> { start };
            return true;
        }

        var queue = new Queue<Tile>();
        var cameFrom = new Dictionary<Tile, Tile>();
        var dist = new Dictionary<Tile, int>();

        queue.Enqueue(start);
        cameFrom[start] = null;
        dist[start] = 0;

        while (queue.Count > 0)
        {
            Tile current = queue.Dequeue();
            if (current == goal) break;

            foreach (Tile neighbor in GetNeighbors(current))
            {
                if (neighbor == null) continue;
                if (!neighbor.IsWalkable) continue;
                if (!CanStep(current, neighbor)) continue;
                if (cameFrom.ContainsKey(neighbor)) continue;

                // Blocca solo se occupata da un ALLEATO del mover, e non è il goal
                if (neighbor != goal
                    && mover != null
                    && neighbor.Occupant != null
                    && neighbor.Occupant.Faction == mover.Faction)
                    continue;

                int nd = dist[current] + 1;
                if (nd > maxDistance) continue;

                cameFrom[neighbor] = current;
                dist[neighbor] = nd;
                queue.Enqueue(neighbor);
            }
        }

        if (!cameFrom.ContainsKey(goal)) return false;

        path = new List<Tile>();
        for (Tile t = goal; t != null; t = cameFrom[t]) path.Add(t);
        path.Reverse();
        return true;
    }

    public List<Tile> GetReachableTiles(Tile start, int maxDistance, Unit mover = null)
    {
        var result = new List<Tile>();
        if (start == null) return result;

        var visited = new HashSet<Tile>();
        var queue = new Queue<(Tile tile, int dist)>();
        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            result.Add(current);

            if (dist >= maxDistance) continue;

            foreach (Tile n in GetNeighbors(current))
            {
                if (n == null || !n.IsWalkable) continue;
                if (!CanStep(current, n)) continue;
                if (visited.Contains(n)) continue;

                // Ignora alleati del mover
                if (mover != null && n.Occupant != null && n.Occupant.Faction == mover.Faction)
                    continue;

                visited.Add(n);
                queue.Enqueue((n, dist + 1));
            }
        }

        return result;
    }

    IEnumerable<Tile> GetNeighbors(Tile tile)
    {
        if (TryGetTile(tile.Coord + Vector2Int.right, out var r)) yield return r;
        if (TryGetTile(tile.Coord + Vector2Int.left, out var l)) yield return l;
        if (TryGetTile(tile.Coord + Vector2Int.up, out var u)) yield return u;
        if (TryGetTile(tile.Coord + Vector2Int.down, out var d)) yield return d;
    }

    public List<Tile> GetReachableTiles(Tile start, int maxDistance)
    {
        var result = new List<Tile>();
        if (start == null) return result;

        var visited = new HashSet<Tile>();
        var queue = new Queue<(Tile tile, int dist)>();
        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            result.Add(current);

            if (dist >= maxDistance) continue;

            foreach (Tile n in GetNeighbors(current))
            {
                if (n == null || !n.IsWalkable) continue;
                if (!CanStep(current, n)) continue; // [MODIFICA] stessa regola di TryGetPath
                if (visited.Contains(n)) continue;
                visited.Add(n);
                queue.Enqueue((n, dist + 1));
            }
        }

        return result;
    }
    
    //si può passare da 'from' a 'to' (adiacenti)? Confronta i bordi che si toccano
    public bool CanStep(Tile from, Tile to)
    {
        if (to.SlopeAngle > maxWalkableSlope) return false;

        Vector2Int dir = to.Coord - from.Coord;
        float fromEdge = from.GetEdgeHeight(dir);
        float toEdge = to.GetEdgeHeight(-dir);
        return Mathf.Abs(fromEdge - toEdge) <= maxStepHeight + 0.001f;
    }
}