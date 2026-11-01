using UnityEngine;

public class PlayerEnergyController : MonoBehaviour
{
    [SerializeField, Min(1)] int energyPerTurn = 3;
    [SerializeField, Min(1)] int energyHardCap = 99;

    FactionEnergyPool pool;

    // Lazy: evita problemi di ordine tra Awake di componenti diversi.
    public FactionEnergyPool Pool => pool ??= new FactionEnergyPool(energyPerTurn, energyHardCap);
}