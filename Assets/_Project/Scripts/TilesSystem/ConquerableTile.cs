using UnityEngine;

public class ConquerableTile : ObjectiveTile
{
    [SerializeField] AreaDefinition area;

    public AreaDefinition Area => area;

    void OnEnable()
    {
        if (AreaManager.HasInstance)
            AreaManager.Instance.Register(this);
    }

    void OnDisable()
    {
        if (AreaManager.HasInstance)
            AreaManager.Instance.Unregister(this);
    }

    public void Conquer(Faction newOwner)
    {
        if (owner == newOwner) return;

        owner = newOwner;
        Debug.Log($"[ConquerableTile] {name} conquistata da {newOwner}");

        if (AreaManager.HasInstance)
            AreaManager.Instance.NotifyTileChanged(this);
    }
}