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
}