using UnityEngine;

[RequireComponent(typeof(GridMover))]
public class Unit : MonoBehaviour
{
    [SerializeField] Faction faction;
    [SerializeField] int initiative;
    [SerializeField] int drawPerTurn = 3;   // NUOVO
    [SerializeField] DeckData startingDeck;

    GridMover mover;
    AbstractHealthManager health;
    AbstractUnitController controller;
    Deck deck;

    public Faction Faction => faction;
    public int Initiative => initiative;
    public int DrawPerTurn => drawPerTurn;
    public GridMover Mover => mover;
    public AbstractHealthManager Health => health;
    public AbstractUnitController Controller => controller;
    public Deck Deck => deck;

    public bool IsAlive => health == null || health.IsAlive;
    public Tile Tile => mover != null ? mover.CurrentTile : null;

    void Awake()
    {
        mover = GetComponent<GridMover>();
        health = GetComponent<AbstractHealthManager>();
        controller = GetComponent<AbstractUnitController>();

        deck = startingDeck != null
            ? new Deck(startingDeck.cards)
            : new Deck(new CardData[0]);
    }
}