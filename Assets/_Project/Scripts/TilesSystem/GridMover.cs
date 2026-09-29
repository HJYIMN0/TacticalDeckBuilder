using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMover : MonoBehaviour
{
    [SerializeField] Tile startTile;
    [SerializeField] float stepDuration = 0.2f;

    Tile currentTile;
    bool isMoving;

    public Tile CurrentTile => currentTile;
    public bool IsMoving => isMoving;

    void Start()
    {
        if (startTile == null)
        {
            Debug.LogError($"[GridMover] {name}: startTile non assegnata", this);
            enabled = false;
            return;
        }

        currentTile = startTile;
        Vector3 p = startTile.transform.position;
        transform.position = new Vector3(p.x, transform.position.y, p.z);

        Unit unit = GetComponent<Unit>();
        if (unit != null) currentTile.SetOccupant(unit);
    }

    public IEnumerator TryMoveAtTiles(Tile destination, int maxDistance)
    {
        if (destination == null) yield break;
        if (isMoving) yield break;
        if (destination == currentTile) yield break;

        if (!TileManager.Instance.TryGetPath(currentTile, destination, maxDistance, out var path))
        {
            Debug.Log($"[GridMover] Nessun percorso verso {destination.Coord} entro {maxDistance}");
            yield break;
        }

        yield return MoveRoutine(path);
    }

    IEnumerator MoveRoutine(List<Tile> path)
    {
        isMoving = true;
        Unit unit = GetComponent<Unit>();

        for (int i = 1; i < path.Count; i++)
        {
            Tile next = path[i];

            currentTile.SetOccupant(null);
            next.SetOccupant(unit);

            yield return StepTo(next);
            currentTile = next;
        }

        isMoving = false;
    }

    IEnumerator StepTo(Tile tile)
    {
        Vector3 start = transform.position;
        Vector3 end = new Vector3(tile.transform.position.x, start.y, tile.transform.position.z);

        for (float t = 0f; t < 1f; t += Time.deltaTime / stepDuration)
        {
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        transform.position = end;
    }
}