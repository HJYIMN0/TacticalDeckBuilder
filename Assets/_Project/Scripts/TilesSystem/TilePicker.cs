using UnityEngine;
using UnityEngine.InputSystem;

public class TilePicker : MonoBehaviour
{
    [SerializeField] LayerMask tileLayer;
    [SerializeField] InputActionReference clickAction;
    [SerializeField] Camera cam;

    public Tile HoveredTile { get; private set; }
    public event System.Action<Tile> TileClicked;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) Debug.LogError("[TilePicker] Nessuna camera");
    }

    void OnEnable()
    {
        if (clickAction == null) { Debug.LogError("[TilePicker] clickAction non assegnata", this); return; }
        clickAction.action.Enable();
        clickAction.action.performed += OnClick;
    }

    void OnDisable()
    {
        if (clickAction == null) return;
        clickAction.action.performed -= OnClick;
    }

    void Update()
    {
        HoveredTile = RaycastTile();
    }

    void OnClick(InputAction.CallbackContext ctx)
    {
        if (HoveredTile != null)
        {
            Debug.Log($"[TilePicker] Click su {HoveredTile.name}{HoveredTile.Coord}");
            TileClicked?.Invoke(HoveredTile);
        }
        else
        {
            Debug.Log("[TilePicker] Click fuori dalla griglia");
        }
    }

    Tile RaycastTile()
    {
        if (Pointer.current == null || cam == null) return null;

        Ray ray = cam.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, tileLayer)) return null;

        return hit.collider.GetComponentInParent<Tile>();
    }
}