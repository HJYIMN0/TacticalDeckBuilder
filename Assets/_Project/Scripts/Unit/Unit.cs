using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMover))]
public class Unit : MonoBehaviour
{
    [SerializeField] Faction faction;
    [SerializeField] int initiative;
    [SerializeField] int drawPerTurn = 3;   // NUOVO
    [SerializeField] DeckData startingDeck;

    [Header("Abilità")]
    [SerializeField] UnitAbility ability;
    public UnitAbility Ability => ability;
    public event Action<Unit, List<CardData>> OnCardsDrawn;

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

        if (health != null)
            health.OnDied += HandleSelfDeath;   // NUOVO

        deck = startingDeck != null
            ? new Deck(startingDeck.cards)
            : new Deck(new CardData[0]);
    }

    /// <summary>
    /// quando QUESTA unità muore, notifica l'abilità del killer
    /// </summary>
    /// <param name="hm"></param>
    /// <param name="killer"></param>
    void HandleSelfDeath(AbstractHealthManager hm, Unit killer)
    {
        if (killer != null && killer != this)
            killer.NotifyUnitKilled(this);

        var t = Tile;
        if (t != null && t.Occupant == this)
        {
            t.SetOccupant(null);
            if (AreaManager.HasInstance)
                AreaManager.Instance.RecountAreasTouching(t, null);   // nessun trigger
        }

        ability?.OnDeath(this);
    }

    public void DrawCards(int n)
    {
        if (deck == null || n <= 0) return;

        int before = deck.Hand.Count;
        deck.Draw(n);

        if (deck.Hand.Count == before) return;

        var newly = new List<CardData>();
        for (int i = before; i < deck.Hand.Count; i++)
            newly.Add(deck.Hand[i]);

        Debug.Log($"[Unit] {name} pesca {newly.Count} carte (bonus)");
        OnCardsDrawn?.Invoke(this, newly);
    }

    // --- Notifiche eventi di gioco ---

    public void NotifyUnitKilled(Unit victim)
    {
        ability?.OnUnitKilled(this, victim);
    }

    public void NotifyObjectiveDestroyed(DestructibleTile objective)
    {
        ability?.OnObjectiveDestroyed(this, objective);
    }

    public void NotifyCardPlayed(CardData card)
    {
        ability?.OnCardPlayed(this, card);
    }

    public void AddCardsToDeck(IEnumerable<CardData> cards)
    {
        if (deck == null) return;
        deck.AddToDrawPile(cards);
    }



    void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleSelfDeath;
    }

}