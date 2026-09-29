using UnityEngine;

public abstract class AbstractHealthManager : MonoBehaviour
{
    [SerializeField] int maxHp = 10;
    int current;

    public int Max => maxHp;
    public int Current => current;
    public bool IsAlive => current > 0;

    void Awake() { current = maxHp; }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        current = Mathf.Max(0, current - amount);
        Debug.Log($"[Health] {name} subisce {amount} -> {current}/{maxHp}");

        if (current <= 0) Die();
    }

    protected abstract void Die();
}