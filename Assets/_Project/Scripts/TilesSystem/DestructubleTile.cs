// FILE: Assets/_Project/Scripts/Tiles/DestructibleTile.cs
// CLASSE: DestructibleTile

using UnityEngine;

public class DestructibleTile : ObjectiveTile
{
    [SerializeField] int maxHp = 10;
    [SerializeField] int currentHp;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsAlive => currentHp > 0;

    protected override void Awake()
    {
        base.Awake();
        currentHp = maxHp;
    }

    public void TakeDamage(int amount, Unit source)
    {
        if (!IsAlive) return;

        currentHp -= amount;
        Debug.Log($"[DestructibleTile] {name} subisce {amount} → {currentHp}/{maxHp}");

        if (currentHp <= 0)
            OnDestroyed(source);
    }

    void OnDestroyed(Unit source)
    {
        Faction winner = source != null ? source.Faction : Faction.Neutral;
        Debug.Log($"[DestructibleTile] {name} distrutta da {source?.name} ({winner})");

        // ✅ notifica l'abilità del killer (se presente)
        if (source != null)
            source.NotifyObjectiveDestroyed(this);

        // TODO: bonus al faction vincitore (energia, carte, ...)

        gameObject.SetActive(false);
    }
}