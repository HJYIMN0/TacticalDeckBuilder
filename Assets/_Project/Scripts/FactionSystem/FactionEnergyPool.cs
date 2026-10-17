using System;
using UnityEngine;

/// <summary>
/// Pool di energia non-singleton. Una istanza vive su PlayerFaction (D3).
/// Non tocca il singleton legacy GlobalEnergyManager.
/// </summary>
public class FactionEnergyPool : IEnergyPool
{
    readonly int energyPerTurn;
    readonly int hardCap;
    int current;

    public FactionEnergyPool(int energyPerTurn, int hardCap = 99)
    {
        this.energyPerTurn = energyPerTurn;
        this.hardCap = hardCap;
    }

    public int Current => current;
    public int EnergyPerTurn => energyPerTurn;
    public event Action Changed;

    public void ResetForTurn() { current = energyPerTurn; Changed?.Invoke(); }
    public bool CanSpend(int a) => a >= 0 && current >= a;
    public bool Spend(int a)
    {
        if (!CanSpend(a)) return false;
        current -= a;
        Changed?.Invoke();
        return true;
    }
    public void Add(int a) { current = Mathf.Min(hardCap, current + a); Changed?.Invoke(); }
}