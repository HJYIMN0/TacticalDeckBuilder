using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMover))]
public class Unit : MonoBehaviour
{
    [Header("Definizione (opzionale)")]
    [SerializeField] UnitDefinition definition;

    [Header("Fallback (usati se definition == null)")]
    [SerializeField] Faction faction;
    [SerializeField] int initiative;

    [Header("Deck locale (SOLO per i nemici; i Player usano PlayerFaction.Deck)")]
    [SerializeField] DeckData startingDeck;
    [SerializeField] int drawPerTurn = 3;

    [Header("Abilità (fallback se definition == null)")]
    [SerializeField] UnitAbility ability;

    public event Action<Unit, List<CardData>> OnCardsDrawn;

    GridMover mover;
    AbstractHealthManager health;
    AbstractUnitController controller;
    Deck deck;
    readonly List<UnitAbility> abilities = new();

    public Faction Faction => definition != null ? definition.faction : faction;
    public int Initiative => definition != null ? definition.initiative : initiative;
    public int DrawPerTurn => drawPerTurn;
    public GridMover Mover => mover;
    public AbstractHealthManager Health => health;
    public AbstractUnitController Controller => controller;
    public Deck Deck => deck;
    public IReadOnlyList<UnitAbility> Abilities => abilities;

    public bool IsAlive => health == null || health.IsAlive;
    public Tile Tile => mover != null ? mover.CurrentTile : null;

    void Awake()
    {
        mover = GetComponent<GridMover>();
        health = GetComponent<AbstractHealthManager>();
        controller = GetComponent<AbstractUnitController>();

        if (definition != null)
        {
            abilities.AddRange(definition.abilities);
            if (health != null)
                health.Configure(definition.maxHp, definition.maxShield, definition.isShieldRenewable);
        }
        else
        {
            if (ability != null) abilities.Add(ability);
        }

        // Deck locale: serve ai nemici. Per i Player è un fallback vuoto
        // (il deck vero è su PlayerFaction), ma lo teniamo creato per non
        // rompere AddCardsToDeck/AreaManager in scenari misti.
        deck = startingDeck != null
            ? new Deck(startingDeck.cards)
            : new Deck(new CardData[0]);

        if (health != null)
            health.OnDied += HandleSelfDeath;
    }

    void HandleSelfDeath(AbstractHealthManager hm, Unit killer)
    {
        if (killer != null && killer != this)
            killer.NotifyUnitKilled(this);

        var t = Tile;
        if (t != null && t.Occupant == this)
        {
            t.SetOccupant(null);
            if (AreaManager.HasInstance)
                AreaManager.Instance.RecountAreasTouching(t, null);
        }

        foreach (var a in abilities) a.OnDeath(this);
    }

    public bool HasAbility<T>() where T : UnitAbility
    {
        foreach (var a in abilities) if (a is T) return true;
        return false;
    }

    public T GetAbility<T>() where T : UnitAbility
    {
        foreach (var a in abilities) if (a is T t) return t;
        return null;
    }

    public IEnumerator RunTurnStartAbilities()
    {
        foreach (var a in abilities)
            yield return a.OnTurnStart(this);
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

    public void NotifyUnitKilled(Unit victim)
    {
        foreach (var a in abilities) a.OnUnitKilled(this, victim);
    }

    public void NotifyObjectiveDestroyed(DestructibleTile objective)
    {
        foreach (var a in abilities) a.OnObjectiveDestroyed(this, objective);
    }

    public void NotifyCardPlayed(CardData card)
    {
        foreach (var a in abilities) a.OnCardPlayed(this, card);
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