using System;

/// <summary>
/// Riserva di energia comune a Player (per fazione) ed Enemy (per unità).
/// I consumer non conoscono il modello sottostante: leggono solo questa interfaccia.
/// </summary>
public interface IEnergyPool
{
    int Current { get; }
    int EnergyPerTurn { get; }
    bool CanSpend(int amount);
    bool Spend(int amount);
    void ResetForTurn();
    void Add(int amount);
    event Action Changed;
}