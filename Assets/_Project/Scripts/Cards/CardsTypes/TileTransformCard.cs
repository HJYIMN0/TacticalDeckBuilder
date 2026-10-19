using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Tile Transform", fileName = "TileTransform_")]
public class TileTransformCard : CardData
{
    [SerializeField] GameObject newTilePrefab;
    [SerializeField] bool moveOccupantToNewTile = true;

    // Sempre TileOnly: nessun executor.
    public override CardTargeting TargetMode => CardTargeting.TileOnly;

    public override bool IsValidTarget(CardContext ctx)
    {
        if (ctx.TargetTile == null) return false;
        if (newTilePrefab == null) return false;
        return true;
    }

    public override IEnumerator Play(CardContext ctx)
    {
        if (!TrySpendCost(ctx)) yield break;   // ← valida + paga

        var oldTile = ctx.TargetTile;
        if (oldTile == null || newTilePrefab == null) yield break;

        var oldGO = oldTile.gameObject;
        var occupant = oldTile.Occupant;
        var pos = oldGO.transform.position;
        var rot = oldGO.transform.rotation;
        var parent = oldGO.transform.parent;

        // 1) rimuovi l'occupante dalla vecchia tile per evitare riferimenti orfani
        if (occupant != null) oldTile.SetOccupant(null);

        // 2) istanzia la nuova tile
        var newGO = Instantiate(newTilePrefab, pos, rot, parent);
        var newTile = newGO.GetComponent<Tile>();

        // 3) aggiorna il TileManager
        //    Assunzione sull'API: esiste un metodo per registrare/sostituire.
        //    Se non esiste, esporne uno (RegisterTile / ReplaceTile) e chiamarlo qui.
        if (newTile != null && TileManager.Instance != null)
        {
            // TODO: adattare alla firma reale di TileManager.
            TileManager.Instance.ReplaceTile(oldTile, newTile);
        }

        // 4) sposta l'occupante sulla nuova tile e aggiorna il mover
        if (moveOccupantToNewTile && occupant != null && newTile != null)
        {
            newTile.SetOccupant(occupant);
            if (occupant.Mover != null)
                occupant.Mover.TeleportTo(newTile);  // vedi nota sotto
        }

        // 5) distruggi la vecchia tile
        Destroy(oldGO);

        Debug.Log($"[TileTransform] tile {oldTile.Coord} → {newTilePrefab.name}");
        yield break;
    }
}