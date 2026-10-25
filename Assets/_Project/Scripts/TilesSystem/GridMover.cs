using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMover : MonoBehaviour
{
    [SerializeField] float stepDuration = 0.2f;
    [SerializeField] LayerMask tileMask;
    [SerializeField][Min(0.1f)] float maxDistanceFromStartTile = 10.5f;

    float groundOffset; // quanto il pivot dell'unità sta sopra la superficie della tile (misurato dal raycast)

    public Tile startTile { get; private set; }

    Tile currentTile;
    bool isMoving;
    bool initialized; // [MODIFICA] true dopo il primo EnsureInitialized()

    public Tile CurrentTile // [MODIFICA] era: public Tile CurrentTile => currentTile;
    {
        get
        {
            EnsureInitialized(); // valida anche se letta prima del Start() di questo GridMover
            return currentTile;
        }
    }

    public bool IsMoving => isMoving;

    void Start()
    {
        EnsureInitialized(); // [MODIFICA] il corpo è passato in EnsureInitialized()
    }

    // [MODIFICA] nuovo: corpo del vecchio Start(), eseguito una sola volta da Start() o dal primo accesso a CurrentTile
    void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, maxDistanceFromStartTile, tileMask))
        {
            startTile = hit.collider.GetComponentInParent<Tile>(); // il raycast restituisce un bool: la Tile si ricava dal collider colpito
            groundOffset = transform.position.y - hit.point.y;     // la Y non è più fissa: si ricorda l'altezza dal suolo
        }

        // se il raggio non trova nulla e non c'è una startTile da Inspector, fermo il componente invece di andare in NullReferenceException
        if (startTile == null)
        {
            Debug.LogError($"[GridMover] {name}: nessuna tile trovata sotto l'oggetto", this);
            enabled = false;
            return;
        }

        currentTile = startTile;
        transform.position = GetStandPosition(startTile); // la Y segue la tile

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
        if (destination == CurrentTile) yield break; // [MODIFICA] passa dalla proprietà
        Unit self = GetComponent<Unit>();
        bool trample = self != null && self.HasAbility<TrampleAbility>();
        
        if (!TileManager.Instance.TryGetPath(CurrentTile, destination, maxDistance, out var path, self, trample))
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
            Unit victim = next.Occupant;
            if (victim != null && victim.Faction != unit.Faction && unit.HasAbility<TrampleAbility>())
            {
                var trample = unit.GetAbility<TrampleAbility>();
                victim.Health.TakeDamage(trample.damage, unit);
            }

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

    /// <summary>
    /// Sposta l'unità istantaneamente su un'altra tile, aggiornando occupant,
    /// currentTile e transform.position. Usato da effetti che teletrasportano
    /// (es. TileTransformCard). Non fa animazioni, non tocca le aree.
    /// </summary>
    public void TeleportTo(Tile destination)
    {
        if (destination == null) return;
        if (isMoving) return;

        Tile origin = currentTile;

        if (currentTile != null) currentTile.SetOccupant(null);
        destination.SetOccupant(GetComponent<Unit>());
        currentTile = destination;
        transform.position = GetStandPosition(destination);

        if (AreaManager.HasInstance)
        {
            AreaManager.Instance.RecountAreasTouching(origin, null);
            AreaManager.Instance.RecountAreasTouching(currentTile, GetComponent<Unit>());
        }
    }

    private void OnDrawGizmos()
    {
        // Punto di partenza del raycast
        Vector3 origin = transform.position + Vector3.up;

        // Disegno il raggio
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector3.down * maxDistanceFromStartTile);

        // Piccola sfera all'origine
        Gizmos.DrawWireSphere(origin, 0.1f);

        // Se siamo in Play Mode e la tile è stata trovata, disegno anche il punto di impatto
        if (Application.isPlaying && startTile != null)
        {
            // Punto di impatto del raycast
            Vector3 hitPoint = startTile.transform.position;
            hitPoint.y = transform.position.y - groundOffset;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hitPoint, 0.15f);

            // Posizione di stand (quella usata dal mover)
            Vector3 standPos = GetStandPosition(startTile);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(standPos, 0.12f);

            // Linea tra hitPoint e standPos
            Gizmos.DrawLine(hitPoint, standPos);
        }
    }

    public IEnumerator JumpTo(Tile destination)
    {
        if (destination == null || isMoving) yield break;
        if (destination == CurrentTile) yield break;
        if (destination.Occupant != null) yield break; // non può atterrare su un'unità
        if (!destination.IsWalkable) yield break;      // non può atterrare su un ostacolo

        isMoving = true;
        Unit unit = GetComponent<Unit>();
        Tile origin = CurrentTile;

        origin.SetOccupant(null);
        destination.SetOccupant(unit);

        Vector3 start = transform.position;
        Vector3 end = GetStandPosition(destination);

        // Arco proporzionale alla lunghezza del salto, così salti lunghi
        // hanno un'arco visibilmente alto sopra l'ostacolo.
        int jumpDist = TileManager.Instance.GetDistance(origin, destination);
        float baseDuration = 0.2f;
        float duration = baseDuration + jumpDist * 0.05f;
        float height = 0.6f + jumpDist * 0.35f;

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * height;
            transform.position = pos;
            yield return null;
        }
        transform.position = end;
        currentTile = destination;
        isMoving = false;

        if (AreaManager.HasInstance)
        {
            AreaManager.Instance.RecountAreasTouching(origin);
            AreaManager.Instance.RecountAreasTouching(currentTile);
        }
    }

    public IEnumerator ForceMoveTo(Tile destination)
    {
        if (destination == null || isMoving) yield break;
        if (destination == CurrentTile) yield break;

        isMoving = true;
        Unit unit = GetComponent<Unit>();
        Tile origin = CurrentTile;

        origin.SetOccupant(null);
        destination.SetOccupant(unit);

        Vector3 start = transform.position;
        Vector3 end = GetStandPosition(destination);
        float duration = 0.2f;

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        transform.position = end;
        currentTile = destination;
        isMoving = false;

        if (AreaManager.HasInstance)
        {
            AreaManager.Instance.RecountAreasTouching(origin);
            AreaManager.Instance.RecountAreasTouching(currentTile);
        }
    }


}