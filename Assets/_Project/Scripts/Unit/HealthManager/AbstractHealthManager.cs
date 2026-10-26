using System;
using UnityEngine;
using UnityEngine.Events;

public abstract class AbstractHealthManager : MonoBehaviour
{
    [SerializeField] int maxHp = 10;
    [SerializeField] int maxShield = 0;
    [SerializeField] bool isShieldRenewable = false;

    int current;
    int currentShield;

    public int Max => maxHp;
    public int Current => current;
    public int MaxShield => maxShield;
    public int CurrentShield => currentShield;
    public bool IsShieldRenewable => isShieldRenewable;
    public bool IsAlive => current > 0;

    public event Action<AbstractHealthManager, Unit> OnDied;
    public UnityEvent<int, int> OnHealthChanged;

    void Awake()
    {
        current = maxHp;
        currentShield = maxShield;
    }
    public virtual void Start()
    {
        OnHealthChanged?.Invoke(current, maxHp);
    }

    public void Configure(int hp, int shield, bool renewable)
    {
        maxHp = hp;
        maxShield = shield;
        isShieldRenewable = renewable;
        current = maxHp;
        currentShield = maxShield;
    }

    public void OnTurnStart()
    {
        if (isShieldRenewable)
            currentShield = maxShield;
    }

    public void AddShield(int amount)
    {
        if (amount <= 0) return;
        currentShield = Mathf.Min(maxShield, currentShield + amount);
        Debug.Log($"[Health] {name} scudo +{amount} -> {currentShield}/{maxShield}");
    }

    public void TakeDamage(int amount, Unit source = null)
    {
        if (!IsAlive) return;

        if (currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, amount);
            currentShield -= absorbed;
            amount -= absorbed;
            Debug.Log($"[Health] {name} scudo assorbe {absorbed} -> scudo {currentShield}/{maxShield}");
        }

        if (amount <= 0) return;

        current = Mathf.Max(0, current - amount);
        Debug.Log($"[Health] {name} subisce {amount} -> {current}/{maxHp}");
        OnHealthChanged?.Invoke(current, maxHp);

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