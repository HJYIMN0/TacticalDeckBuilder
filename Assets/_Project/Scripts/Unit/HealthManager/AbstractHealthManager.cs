using System;
using UnityEngine;

public abstract class AbstractHealthManager : MonoBehaviour
{
    [SerializeField] int maxHp = 10;
    int current;

    public int Max => maxHp;
    public int Current => current;
    public bool IsAlive => current > 0;

    /// <summary>Notifica quando questa entità muore. Passa il killer (può essere null).</summary>
    public event Action<AbstractHealthManager, Unit> OnDied;

    void Awake() { current = maxHp; }

    public void TakeDamage(int amount, Unit source = null)
    {
        if (!IsAlive) return;

        current = Mathf.Max(0, current - amount);
        Debug.Log($"[Health] {name} subisce {amount} -> {current}/{maxHp}");

        if (current <= 0)
        {
            Die();
            OnDied?.Invoke(this, source);
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        current = Mathf.Min(maxHp, current + amount);
        Debug.Log($"[Health] {name} cura {amount} -> {current}/{maxHp}");
    }

    protected abstract void Die();
}