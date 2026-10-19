using System;
using UnityEngine;

public class GlobalEnergyManager : GenericSingleton<GlobalEnergyManager>, IEnergyPool
{
    public override bool IsDestroyedOnLoad() => true;
    public override bool ShouldDetatchFromParent() => true;

    [SerializeField] int energyPerTurn = 3;
    [SerializeField] int hardCap = 99;
    [SerializeField] private bool isDebugMode = false;
    public int Current { get; private set; }
    public int EnergyPerTurn => energyPerTurn;

    public event Action Changed;

    public override void Awake()
    {
        base.Awake();

        Current = energyPerTurn;   // inizializza il prima possibile
    }

    private void Start()
    {
        // log dopo l'init, così mostra il valore giusto
        Debug.Log($"[GlobalEnergyManager] current energy: {Current} max: {energyPerTurn}");
        Changed?.Invoke(); // notifica chi si era iscritto in OnEnable
    }

    public void ResetForTurn()
    {
        Current = energyPerTurn;
        Changed?.Invoke();
    }

    public bool CanSpend(int amount)
    {
        if (isDebugMode) return true;
        return amount >= 0 && Current >= amount;
    }

    public bool Spend(int amount)
    {
        if (!CanSpend(amount)) return false;
        Current -= amount;
        int subs = Changed?.GetInvocationList().Length ?? 0;
        Debug.Log($"[GlobalEnergyManager] Spend({amount}) -> Current={Current}, subscribers={subs}, id={GetEntityId()}");
        Changed?.Invoke();
        return true;
    }

    public void Add(int amount)
    {
        Current = Mathf.Min(hardCap, Current + amount);
        Changed?.Invoke();
    }
}