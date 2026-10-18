using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [Tooltip("Se true, appena selezionato un target valido la carta viene eseguita. " +
             "Se false serve ConfirmPlay().")]
    [SerializeField] bool autoConfirmOnSelection = true;

    [Header("Input")]
    [Tooltip("Azione che annulla la selezione della carta corrente (es. Escape). " +
         "Abilitata solo durante il turno Player.")]
    [SerializeField] InputActionReference cancelSelection;

    public event Action<CardView> OnCardSelected;
    public event Action<Unit> OnUnitSelected;
    public event Action<Tile> OnTileSelected;
    public event Action OnConfirm;
    public event Action OnCancel;

    readonly PlayerTurnState state = new();
    List<Unit> factionUnits = new();
    bool endTurnRequested;
    bool pendingResolution;

    public bool IsConfigured =>
        picker != null && handView != null &&
        unitSelector != null && faction != null;

    public override IEnumerator TakeTurn(Unit unit)
    {
        Debug.LogWarning("[Player] TakeTurn(Unit) deprecato. Il turno è di fazione.");
        yield break;
    }

    // ===================================================================
    // Turno
    // ===================================================================
    public IEnumerator TakeFactionTurn(IReadOnlyList<Unit> units)
    {
        if (!IsConfigured)
        {
            Debug.LogError("[Player] Setup mancante", this);
            yield break;
        }

        factionUnits = units.Where(u => u != null && u.IsAlive).ToList();
        if (factionUnits.Count == 0) { Debug.LogWarning("[Player] Nessuna unità viva"); yield break; }

        state.ResetAll();
        endTurnRequested = false;
        pendingResolution = false;

        Debug.Log($"[Player] === Turno fazione Player | {factionUnits.Count} unità ===");

        faction.Energy.ResetForTurn();
        if (AreaManager.HasInstance)
            AreaManager.Instance.ApplyBonusesFor(Faction.Player, faction.Energy);

        faction.Deck.Draw(faction.DrawPerTurn);
        foreach (var c in faction.Deck.Hand)
            handView.SpawnAt(c, Vector2.down * 600f);
        yield return handView.AnimateAllToHome();

        // Bottoni SEMPRE visibili, unità di default = prima
        unitSelector.ShowAll(factionUnits, u => u != null && u.IsAlive);
        SetActiveUnit(factionUnits[0]);

        handView.CardClicked += HandleCardClicked;
        picker.TileClicked += HandleTileClicked;
        unitSelector.UnitSelected += HandleUnitSelected;
        TurnManager.Instance.EndTurnRequested += HandleEndTurnRequested;

        if (cancelSelection != null && cancelSelection.action != null)
        {
            cancelSelection.action.Enable();
            cancelSelection.action.performed += HandleCancelInput;
        }

        while (!endTurnRequested)
        {
            yield return null;
            if (pendingResolution)
            {
                pendingResolution = false;
                yield return ResolvePending();
            }
        }

        handView.CardClicked -= HandleCardClicked;
        picker.TileClicked -= HandleTileClicked;
        unitSelector.UnitSelected -= HandleUnitSelected;
        TurnManager.Instance.EndTurnRequested -= HandleEndTurnRequested;

        if (cancelSelection != null && cancelSelection.action != null)
        {
            cancelSelection.action.performed -= HandleCancelInput;
            cancelSelection.action.Disable();
        }

        CancelSelection();
        if (pathPreview != null) pathPreview.ClearAll();
        unitSelector.Clear();

        var snapshot = new List<CardView>(handView.Views);
        foreach (var v in snapshot)
        {
            if (v == null) continue;
            faction.Deck.Discard(v.Card);
            yield return handView.AnimateDiscard(v);
        }

        state.ResetAll();
    }

    void HandleEndTurnRequested() => endTurnRequested = true;

    void HandleCancelInput(InputAction.CallbackContext ctx)
    {
        // Niente da annullare in Idle o Resolving
        if (state.Phase != PlayerTurnPhase.CardSelected) return;
        if (state.Card == null) return;

        Debug.Log("[Player] Cancel selection (input)");
        CancelSelection();
    }

    // ===================================================================
    // Selezione unità (persistente, sempre attiva)
    // ===================================================================
    void SetActiveUnit(Unit unit)
    {
        if (unit == null) return;
        state.ActiveUnit = unit;

        unitSelector.SetActive(unit);
        if (pathPreview != null) pathPreview.SetUnit(unit);
        if (tileHighlight != null && unit.Mover != null)
            tileHighlight.SetActiveMover(unit.Mover);

        OnUnitSelected?.Invoke(unit);
    }

    void HandleUnitSelected(Unit unit)
    {
        if (unit == null || !unit.IsAlive) return;
        if (state.Phase == PlayerTurnPhase.Resolving) return;

        switch (state.Phase)
        {
            case PlayerTurnPhase.Idle:
                SetActiveUnit(unit);
                return;

            case PlayerTurnPhase.CardSelected:
                if (state.Card == null) return;

                switch (state.Card.Card.TargetMode)
                {
                    case CardTargeting.ExecutorAndTile:
                        // cambia esecutore, tile highlight si aggiorna
                        SetActiveUnit(unit);
                        break;

                    case CardTargeting.UnitOnly:
                        // unità = target → esegue
                        state.TargetTile = unit.Tile;
                        pendingResolution = true;
                        break;

                    case CardTargeting.TileOnly:
                        // target è tile, l'unità non conta
                        SetActiveUnit(unit);
                        break;
                }
                return;
        }
    }

    // ===================================================================
    // Selezione carta
    // ===================================================================
    void HandleCardClicked(CardView view)
    {
        if (view == null || view.Card == null) return;
        if (state.Phase == PlayerTurnPhase.Resolving) return;

        // Stessa carta → deseleziona
        if (state.Card == view)
        {
            CancelSelection();
            return;
        }

        if (state.Card != null) state.Card.Deselect();

        state.Card = view;
        state.TargetTile = null;
        state.Phase = PlayerTurnPhase.CardSelected;
        view.Select();

        if (pathPreview != null) pathPreview.SetCard(view.Card);
        OnCardSelected?.Invoke(view);
    }

    // ===================================================================
    // Click su tile
    // ===================================================================
    void HandleTileClicked(Tile tile)
    {
        if (tile == null) return;
        if (state.Phase == PlayerTurnPhase.Resolving) return;

        // Idle: click su tile con unità Player → cambia active unit
        if (state.Phase == PlayerTurnPhase.Idle)
        {
            var occ = tile.Occupant;
            if (occ != null && occ.Faction == Faction.Player
                && factionUnits.Contains(occ) && occ.IsAlive)
            {
                SetActiveUnit(occ);
            }
            return;
        }

        // CardSelected
        if (state.Phase != PlayerTurnPhase.CardSelected || state.Card == null) return;

        var mode = state.Card.Card.TargetMode;

        if (mode == CardTargeting.UnitOnly)
        {
            var occ = tile.Occupant;
            if (occ != null && occ.Faction == Faction.Player
                && factionUnits.Contains(occ) && occ.IsAlive)
            {
                state.TargetTile = tile;
                pendingResolution = true;
            }
            return;
        }

        // Tile-targeting (ExecutorAndTile / TileOnly)
        state.TargetTile = tile;
        OnTileSelected?.Invoke(tile);

        if (autoConfirmOnSelection) pendingResolution = true;
    }

    // ===================================================================
    // Conferma / Annulla
    // ===================================================================
    public void ConfirmPlay()
    {
        if (state.Card == null) return;
        if (state.Phase != PlayerTurnPhase.CardSelected) return;
        if (state.TargetTile == null) return;
        pendingResolution = true;
    }

    public void CancelSelection()
    {
        if (state.Card != null) state.Card.Deselect();
        if (pathPreview != null) pathPreview.ClearCard();
        if (tileHighlight != null) tileHighlight.ClearAllHighlights();
        state.Reset();   // mantiene ActiveUnit
        OnCancel?.Invoke();
    }

    // ===================================================================
    // Risoluzione
    // ===================================================================
    IEnumerator ResolvePending()
    {
        var card = state.Card;
        if (card == null) yield break;

        var user = state.ActiveUnit;
        var targetTile = state.TargetTile;

        if (targetTile == null && card.Card.TargetMode == CardTargeting.UnitOnly && user != null)
            targetTile = user.Tile;

        if (!faction.Energy.CanSpend(card.Card.cost))
        {
            Debug.Log("[Player] Energia insufficiente");
            card.Shake();
            yield break;
        }

        var ctx = new CardContext
        {
            User = card.Card.TargetMode == CardTargeting.TileOnly ? null : user,
            TargetTile = targetTile,
            Energy = faction.Energy
        };

        if (!card.Card.IsValidTarget(ctx))
        {
            Debug.Log($"[Player] Target non valido per {card.Card.cardName}");
            card.Shake();
            yield break;
        }

        faction.Energy.Spend(card.Card.cost);
        if (pathPreview != null) pathPreview.ClearCard();
        if (tileHighlight != null) tileHighlight.ClearAllHighlights();
        state.Phase = PlayerTurnPhase.Resolving;

        yield return card.Card.Play(ctx);

        faction.Deck.Discard(card.Card);
        yield return handView.AnimateDiscard(card);

        state.Reset();   // mantiene ActiveUnit, si torna a Idle
        OnConfirm?.Invoke();
    }
}