using UnityEngine;

/// <summary>
/// Stato di fazione Player: deck condiviso e draw/turn.
/// L'energia vive in PlayerEnergyController. Nessun singleton.
/// </summary>
public class PlayerFaction : MonoBehaviour
{
    [SerializeField] DeckData startingDeck;
    [SerializeField, Min(1)] int drawPerTurn = 3;
    [SerializeField] PlayerEnergyController energyController;

    Deck deck;

    public Deck Deck => deck;
    public int DrawPerTurn => drawPerTurn;
    public IEnergyPool Energy => energyController.Pool;

    void Awake()
    {
        deck = startingDeck != null
            ? new Deck(startingDeck.cards)
            : new Deck(new CardData[0]);

        if (energyController == null) energyController = GetComponent<PlayerEnergyController>();
        if (energyController == null)
            Debug.LogError("[PlayerFaction] PlayerEnergyController mancante sullo stesso GameObject", this);
    }
}