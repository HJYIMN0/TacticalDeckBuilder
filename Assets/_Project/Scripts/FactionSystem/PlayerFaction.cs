using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stato di fazione Player: deck condiviso, draw/turn, energia condivisa.
/// Vive su un GameObject scena dedicato ("PlayerFaction"). Nessun singleton.
/// </summary>
public class PlayerFaction : MonoBehaviour
{
    [SerializeField] DeckData startingDeck;
    [SerializeField, Min(1)] int drawPerTurn = 3;
    [SerializeField, Min(1)] int energyPerTurn = 3;
    [SerializeField, Min(1)] int energyHardCap = 99;

    Deck deck;
    FactionEnergyPool energy;

    public Deck Deck => deck;
    public int DrawPerTurn => drawPerTurn;
    public IEnergyPool Energy => energy;

    void Awake()
    {
        deck = startingDeck != null
            ? new Deck(startingDeck.cards)
            : new Deck(new CardData[0]);
        energy = new FactionEnergyPool(energyPerTurn, energyHardCap);
    }
}