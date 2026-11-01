using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TileHoverController : MonoBehaviour
{
    [SerializeField] InputActionReference pointAction;   // es. UI/Point
    [SerializeField] Camera worldCamera;
    [SerializeField] LayerMask hoverMask;                // layer di Tile e Unit
    [SerializeField] float maxDistance = 200f;
    [SerializeField] TileHighlightProfile profile;

    Collider lastCollider;
    Tile colliderTile;
    Unit colliderUnit;

    Tile lastTile;
    TileHighlight lastHighlight;
    TileHoverKind lastKind;

    DestructibleTile lastDestructible;

    void OnEnable()
    {
        // Enable è idempotente: non faccio Disable perché l'azione è condivisa con la UI.
        if (pointAction != null) pointAction.action.Enable();
        if (worldCamera == null) worldCamera = Camera.main;
    }

    void OnDisable() => ClearCurrent();

    void Update()
    {
        Tile tile = GetTileUnderPointer();

        if (tile != lastTile)
        {
            ClearCurrent();
            lastTile = tile;
            // Lookup solo al cambio di tile, mai per frame.
            lastHighlight = null;
            lastDestructible = null;
            if (tile != null)
            {
                tile.TryGetComponent(out lastHighlight);
                tile.TryGetComponent(out lastDestructible);
            }
            lastKind = TileHoverKind.None;
        }

        CardData card = CardView.CurrentSelected != null ? CardView.CurrentSelected.Card : null;
        TileHoverKind kind = tile != null ? Resolve(tile, lastDestructible, card) : TileHoverKind.None;

        if (kind == lastKind) return;
        lastKind = kind;

        if (lastHighlight == null) return;
        if (kind == TileHoverKind.None) lastHighlight.ClearHover();
        else lastHighlight.ShowHover(kind, profile);
    }

    Tile GetTileUnderPointer()
    {
        if (pointAction == null || worldCamera == null) return null;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            lastCollider = null;
            return null;
        }

        Vector2 p = pointAction.action.ReadValue<Vector2>();
        Ray ray = worldCamera.ScreenPointToRay(p);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, hoverMask))
        {
            lastCollider = null;
            return null;
        }

        if (hit.collider != lastCollider)
        {
            lastCollider = hit.collider;
            colliderTile = hit.collider.GetComponentInParent<Tile>();
            colliderUnit = colliderTile == null ? hit.collider.GetComponentInParent<Unit>() : null;
        }

        if (colliderTile != null) return colliderTile;
        return colliderUnit != null ? colliderUnit.Tile : null;   // la tile dell'unità può cambiare: letta ogni frame
    }

    TileHoverKind Resolve(Tile tile, DestructibleTile destructible, CardData card)
    {
        Unit u = tile.Occupant;

        if (u == null || !u.IsAlive)
        {
            if (destructible != null && destructible.IsAlive) return TileHoverKind.Destructible;   // F
            return card == null ? TileHoverKind.EmptyTile : TileHoverKind.None;                    // B
        }

        if (card == null)
        {
            if (u.Faction == Faction.Player) return TileHoverKind.PlayerUnit;                      // A
            if (u.Faction == Faction.Enemy) return TileHoverKind.EnemyUnit;                        // C
            return TileHoverKind.None;
        }

        if (card is AbstractAttackCard && u.Faction == Faction.Enemy)
            return TileHoverKind.EnemyAttackTarget;                                                // D

        return TileHoverKind.None;
    }

    void ClearCurrent()
    {
        if (lastHighlight != null) lastHighlight.ClearHover();
    }
}