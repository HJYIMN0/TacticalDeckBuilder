using UnityEngine;

public abstract class ObjectiveTile : MonoBehaviour
{
    [SerializeField] protected Faction owner = Faction.Neutral;
    [Tooltip("Priorità per l'AI nemica (più alto = più attraente)")]
    [SerializeField] protected int aiPriority = 1;

    protected Tile tile;

    public Faction Owner => owner;
    public int AiPriority => aiPriority;
    public Tile Tile => tile;

    protected virtual void Awake()
    {
        tile = GetComponent<Tile>();
        if (tile == null)
            Debug.LogWarning($"[ObjectiveTile] {name}: manca componente Tile");
    }

    public virtual void SetOwner(Faction newOwner)
    {
        owner = newOwner;
    }
}