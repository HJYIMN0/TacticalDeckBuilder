    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class GridMover : MonoBehaviour
    {
        [SerializeField] float stepDuration = 0.2f;
        [SerializeField] LayerMask tileMask;
        [SerializeField] float maxDistancedFromStartTile;

        float groundOffset; // quanto il pivot dell'unità sta sopra la superficie della tile (misurato in Start dal raycast)

        public Tile startTile { get; private set; }
    
        Tile currentTile;
        bool isMoving;

        public Tile CurrentTile => currentTile;
        public bool IsMoving => isMoving;

    void Start()
    {
        if (Physics.Raycast(transform.position + Vector3.up * maxDistancedFromStartTile, Vector3.down, out RaycastHit hit, 10.5f, tileMask))
        {
            startTile = hit.collider.GetComponentInParent<Tile>(); // il raycast restituisce un bool: la Tile si ricava dal collider colpito
            groundOffset = transform.position.y - hit.point.y;     // la Y non è più fissa: si ricorda l'altezza dal suolo
        }

        //  se il raggio non trova nulla e non c'è una startTile da Inspector, fermo il componente invece di andare in NullReferenceException
        if (startTile == null)
        {
            Debug.LogError($"[GridMover] {name}: nessuna tile trovata sotto l'oggetto", this);
            enabled = false;
            return;
        }

        currentTile = startTile;
        transform.position = GetStandPosition(startTile); // prima: Y del transform invariata; ora la Y segue la tile

        Unit unit = GetComponent<Unit>();
        if (unit != null) currentTile.SetOccupant(unit);
    }
    // centro della superficie della tile + offset dal suolo
    Vector3 GetStandPosition(Tile tile)
    {
        return tile.transform.position + Vector3.up * groundOffset;
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
            Tile origin = currentTile;

            for (int i = 1; i < path.Count; i++)
            {
                Tile next = path[i];

                currentTile.SetOccupant(null);
                next.SetOccupant(unit);

                yield return StepTo(next);
                currentTile = next;
            }

            isMoving = false;

            // Rivaluta le aree coinvolte: tile di partenza e di arrivo.
            // Origin serve per il caso "unità esce da un'area".
            if (AreaManager.HasInstance)
            {
                Debug.Log($"[GridMover] {unit?.name} termina su {currentTile?.Coord} — riconto aree");
                AreaManager.Instance.RecountAreasTouching(origin);
                AreaManager.Instance.RecountAreasTouching(currentTile);
            }
            else
            {
                Debug.LogWarning("[GridMover] AreaManager non presente: conquista non valutata");
            }

            // Verifichiamo se ci sono bonus da assegnare per l'area di arrivo (solo se l'unità è entrata in un'area)
            if (AreaManager.HasInstance)
            {
                // origin: l'unità è USCITA → nessun trigger (nessuno conquista uscendo)
                AreaManager.Instance.RecountAreasTouching(origin, null);

                // destinazione: l'unità è ENTRATA → è il trigger
                AreaManager.Instance.RecountAreasTouching(currentTile, unit);
            }
            isMoving = false;
        }

        IEnumerator StepTo(Tile tile)
        {
            Vector3 start = transform.position;
            Vector3 end = GetStandPosition(tile); // anche la Y arriva dalla tile

            for (float t = 0f; t < 1f; t += Time.deltaTime / stepDuration)
            {
                transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
            transform.position = end;
        }

}