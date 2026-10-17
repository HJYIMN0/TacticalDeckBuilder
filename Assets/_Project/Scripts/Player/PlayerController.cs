using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : AbstractUnitController
{
    [Header("Scene refs")]
    [SerializeField] TilePicker picker;
    [SerializeField] HandView handView;
    [SerializeField] PathPreview pathPreview;
    [SerializeField] UnitSelectorUI unitSelector;
    [SerializeField] TileHighlightController tileHighlight;
    [SerializeField] PlayerFaction faction;

    [Header("Behavior")]
    [Tooltip("Se true, un click su una tile/unità valida conferma subito. Se false, aspetta ConfirmPlay().")]
    [SerializeField] bool autoConfirmOnSelection = true;

    // Eventi UI (D: preferire eventi a chiamate dirette)
    public event Action<CardView> OnCardSelected;
    public event Action<Unit> OnUnitSelected;
    public event Action<Tile> OnTileSelected;
    public event Action OnConfirm;
    public event Action OnCancel;

    readonly PlayerTurnState state = new();
    List<Unit> factionUnits = new();
    bool endTurnRequested;
    bool pendingResolution;

    // ---------------------------------------------------------------
    // AbstractUnitController — non più usato per il Player.
    // ---------------------------------------------------------------
    public override IEnumerator TakeTurn(Unit unit)
    {
        Debug.LogWarning("[Player] TakeTurn(Unit) deprecato. Il turno è di fazione.");
        yield break;
    }

    // ---------------------------------------------------------------
    // Turno di fazione
    // ---------------------------------------------------------------
    public IEnumerator TakeFactionTurn(IReadOnlyList<Unit> units)
    {
        if (handView == null || picker == null || unitSelector == null || faction == null)
        {
            Debug.LogError("[Player] Setup mancante (handView/picker/unitSelector/faction)");
            yield break;
        }

        factionUnits = units.Where(u => u != null && u.IsAlive).ToList();
        state.Reset();
        endTurnRequested = false;
        pendingResolution = false;

        Debug.Log($"[Player] === Turno fazione Player | {factionUnits.Count} unità ===");

        // Energia di fazione
        faction.Energy.ResetForTurn();
        if (AreaManager.HasInstance)
            AreaManager.Instance.ApplyBonusesFor(Faction.Player, faction.Energy);

        // Pesca di fazione
        faction.Deck.Draw(faction.DrawPerTurn);
        foreach (var c in faction.Deck.Hand)
            handView.SpawnAt(c, Vector2.down * 600f);

        yield return handView.AnimateAllToHome();

        // Sottoscrizioni
        handView.CardClicked += HandleCardClicked;
        picker.TileClicked += HandleTileClicked;
        unitSelector.UnitSelected += HandleUnitSelected;
        TurnManager.Instance.EndTurnRequested += HandleEndTurnRequested;

        // Loop principale
        while (!endTurnRequested)
        {
            yield return null;
            if (pendingResolution)
            {
                pendingResolution = false;
                yield return ResolvePending();
            }
        }

        // Cleanup
        handView.CardClicked -= HandleCardClicked;
        picker.TileClicked -= HandleTileClicked;
        unitSelector.UnitSelected -= HandleUnitSelected;
        TurnManager.Instance.EndTurnRequested -= HandleEndTurnRequested;

        CancelSelection();
        if (pathPreview != null) pathPreview.ClearAll();
        unitSelector.Clear();

        // Scarto mano residua
        var snapshot = new List<CardView>(handView.Views);
        foreach (var v in snapshot)
        {
            if (v == null) continue;
            faction.Deck.Discard(v.Card);
            yield return handView.AnimateDiscard(v);
        }

        Debug.Log("[Player] === Fine turno fazione Player ===");
    }

    // ---------------------------------------------------------------
    // Input handlers
    // ---------------------------------------------------------------
    void HandleEndTurnRequested() => endTurnRequested = true;

    void HandleCardClicked(CardView view)
    {
        if (view == null || view.Card == null) return;
        if (state.Phase != PlayerTurnPhase.Idle) return;

        state.Card = view;
        view.Select();

        switch (view.Card.TargetMode)
        {
            case CardTargeting.ExecutorAndTile:
                state.Phase = PlayerTurnPhase.SelectingExecutor;
                unitSelector.ShowAll(factionUnits, u => view.Card.CanBeExecutedBy(u));
                break;

            case CardTargeting.UnitOnly:
                state.Phase = PlayerTurnPhase.SelectingTargetUnit;
                unitSelector.ShowAll(factionUnits, u => view.Card.CanBeExecutedBy(u));
                break;

            case CardTargeting.TileOnly:
                state.Phase = PlayerTurnPhase.SelectingTile;
                break;
        }

        if (pathPreview != null) pathPreview.SetCard(view.Card);
        OnCardSelected?.Invoke(view);
    }

    void HandleUnitSelected(Unit unit)
    {
        if (state.Card == null || unit == null) return;

        if (state.Phase == PlayerTurnPhase.SelectingExecutor)
        {
            if (!state.Card.Card.CanBeExecutedBy(unit)) return;

            state.Executor = unit;
            state.Phase = PlayerTurnPhase.SelectingTile;
            if (pathPreview != null) pathPreview.SetUnit(unit);
            if (tileHighlight != null && unit.Mover != null)
                tileHighlight.SetActiveMover(unit.Mover);

            OnUnitSelected?.Invoke(unit);
        }
        else if (state.Phase == PlayerTurnPhase.SelectingTargetUnit)
        {
            if (!state.Card.Card.CanBeExecutedBy(unit)) return;

            state.TargetUnit = unit;
            state.Executor = unit;   // unità = esecutore = target
            state.Phase = PlayerTurnPhase.AwaitingConfirm;

            OnUnitSelected?.Invoke(unit);
            if (autoConfirmOnSelection) pendingResolution = true;
        }
    }

    void HandleTileClicked(Tile tile)
    {
        if (state.Card == null || tile == null) return;
        if (state.Phase != PlayerTurnPhase.SelectingTile) return;

        state.TargetTile = tile;
        state.Phase = PlayerTurnPhase.AwaitingConfirm;

        OnTileSelected?.Invoke(tile);
        if (autoConfirmOnSelection) pendingResolution = true;
    }

    // ---------------------------------------------------------------
    // API pubblica per UI (Conferma / Annulla)
    // ---------------------------------------------------------------
    public void ConfirmPlay()
    {
        if (state.Card == null) return;
        if (state.Phase != PlayerTurnPhase.AwaitingConfirm) return;
        pendingResolution = true;
    }

    public void CancelSelection()
    {
        if (state.Card != null) state.Card.Deselect();
        if (pathPreview != null)
        {
            pathPreview.ClearCard();
            pathPreview.ClearTargetUnit();
        }
        if (tileHighlight != null) tileHighlight.ClearAllHighlights();
        state.Reset();
        unitSelector.Clear();
        OnCancel?.Invoke();
    }

    // ---------------------------------------------------------------
    // Risoluzione
    // ---------------------------------------------------------------
    IEnumerator ResolvePending()
    {
        var card = state.Card;
        if (card == null) yield break;

        var user = state.Executor;
        var targetUnit = state.TargetUnit;
        var targetTile = state.TargetTile ?? targetUnit?.Tile;

        if (!faction.Energy.CanSpend(card.Card.cost))
        {
            Debug.Log("[Player] Energia insufficiente");
            card.Shake();
            state.Phase = PlayerTurnPhase.AwaitingConfirm;
            yield break;
        }

        var ctx = new CardContext
        {
            User = user,
            TargetTile = targetTile,
            Energy = faction.Energy
        };

        if (!card.Card.IsValidTarget(ctx))
        {
            Debug.Log($"[Player] Target non valido per {card.Card.cardName}");
            card.Shake();
            state.Phase = PlayerTurnPhase.AwaitingConfirm;
            yield break;
        }

        faction.Energy.Spend(card.Card.cost);
        if (pathPreview != null) pathPreview.ClearCard();
        if (tileHighlight != null) tileHighlight.ClearAllHighlights();
        state.Phase = PlayerTurnPhase.Resolving;

        yield return card.Card.Play(ctx);

        faction.Deck.Discard(card.Card);
        yield return handView.AnimateDiscard(card);

        state.Reset();
        OnConfirm?.Invoke();
    }
}