using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

public class PlayerController : AbstractUnitController
{
    [SerializeField] TilePicker picker;
    [SerializeField] MultiHandView handView;
    [SerializeField] PathPreview pathPreview;
    [SerializeField] UnitSelectorUI unitSelector;
    [SerializeField] GlobalEnergyManager energy; // opzionale: fallback su singleton

    PlayerAction pending;
    bool hasPending;

    List<Unit> controlledUnits;
    Unit activeUnit;
    CardView activeCard;

    GlobalEnergyManager Energy => energy != null ? energy : GlobalEnergyManager.Instance;
    Coroutine switchRoutine;
    bool isSwitching;
    bool isResolvingCard;

    // CLASSE: PlayerController
    public override IEnumerator TakeTurn(Unit unit)
    {
        if (picker == null || handView == null || unitSelector == null)
        {
            Debug.LogError("[Player] Setup mancante (picker/handView/unitSelector)");
            yield break;
        }
        if (Energy == null)
        {
            Debug.LogError("[Player] GlobalEnergyManager non trovato");
            yield break;
        }

        // --- Raccogli tutte le unità della fazione guidata da questo player ---
        controlledUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude)
            .Where(u => u.Faction == unit.Faction
                     && u.Controller is PlayerController
                     && u.IsAlive)
            .ToList();

        Debug.Log($"[Player] === Inizio turno fazione {unit.Faction} | {controlledUnits.Count} unità ===");

        // --- Energia globale: reset ---
        Energy.ResetForTurn();

        if (AreaManager.HasInstance)
            AreaManager.Instance.ApplyBonusesFor(unit.Faction, Energy);

        // --- Pesca per ogni unità ---
        foreach (var u in controlledUnits)
        {
            u.Deck.Draw(u.DrawPerTurn);
            var hand = handView.GetOrCreate(u);

            // la mano nasce nascosta (GetOrCreate): la attiviamo solo il tempo
            // necessario a SpawnAt per piazzare le carte.
            hand.gameObject.SetActive(true);

            foreach (var c in u.Deck.Hand)
                hand.SpawnAt(c, Vector2.down * 600f);

        }

        // nascondi TUTTE le mani in un colpo solo (istantaneo) e azzera activeUnit di MultiHandView
        handView.HideAll();

        // --- Mostra SOLO la mano della prima unità, con animazione ---
        yield return handView.ShowHand(controlledUnits[0]);

        // [MODIFICA] unico blocco di attivazione della prima unità.
        // Rimossi: il primo blocco (SetActive selector + SetUnit prima di Build) e
        // handView.SetActive(...) dopo ShowHand: ShowHand imposta già activeUnit e mostra la mano.
        activeUnit = controlledUnits[0];

        // --- UI selezione unità ---
        unitSelector.Build(controlledUnits, controlledUnits[0]);
        unitSelector.UnitSelected += OnUnitSelected;
        unitSelector.SetActive(controlledUnits[0]);
        if (pathPreview != null) pathPreview.SetUnit(controlledUnits[0]);

        // --- Loop principale ---
        bool endTurn = false;
        while (!endTurn)
        {
            yield return WaitForAction();

            var action = pending;
            hasPending = false;

            switch (action.Type)
            {
                case PlayerAction.Kind.EndTurn:
                    Debug.Log("[Player] End turn ricevuto");
                    if (activeCard != null) { activeCard.Deselect(); activeCard = null; }
                    if (pathPreview != null) pathPreview.ClearCard();
                    endTurn = true;
                    break;

                case PlayerAction.Kind.CardClicked:
                    HandleCardClick(action.CardView);
                    break;

                case PlayerAction.Kind.TileClicked:
                    yield return HandleTileClick(action.TileTarget);
                    break;
            }
        }

        // --- Cleanup evento UI ---
        unitSelector.UnitSelected -= OnUnitSelected;

        // --- Scarta le mani residue di ogni unità ---
        foreach (var u in controlledUnits)
        {
            var hand = handView.GetOrCreate(u);
            var snapshot = new List<CardView>(hand.Views);
            foreach (var v in snapshot)
            {
                u.Deck.Discard(v.Card);
                yield return hand.AnimateDiscard(v);
            }
        }

        unitSelector.Clear();
        handView.ClearAll();

        Debug.Log("[Player] === Fine turno ===");
    }

    // -------------------------------------------------------------------------

    void SetActiveUnit(Unit u)
    {
        if (u == activeUnit) return;

        if (activeCard != null) { activeCard.Deselect(); activeCard = null; }

        activeUnit = u;
        handView.SetActive(u);
        unitSelector.SetActive(u);

        if (pathPreview != null)
        {
            pathPreview.SetUnit(u);
            pathPreview.ClearCard();
        }

        Debug.Log($"[Player] Unità attiva: {u.name}");
    }

    void OnUnitSelected(Unit u)
    {
        if (u == activeUnit || isSwitching || isResolvingCard) return;
        if (switchRoutine != null) StopCoroutine(switchRoutine);
        switchRoutine = StartCoroutine(SwitchToUnit(u));
    }

    IEnumerator SwitchToUnit(Unit u)
    {
        isSwitching = true;

        if (activeCard != null) { activeCard.Deselect(); activeCard = null; }
        if (pathPreview != null) pathPreview.ClearCard();

        yield return handView.SetActiveAnimated(u);   // <-- niente handView.enabled

        activeUnit = u;
        unitSelector.SetActive(u);
        if (pathPreview != null) pathPreview.SetUnit(u);

        isSwitching = false;
        switchRoutine = null;

        Debug.Log($"[Player] Unità attiva: {u.name}");
    }

    void HandleCardClick(CardView view)
    {
        if (view == null) return;
        if (isSwitching) return;
        if (activeCard == view) { Debug.Log($"[Player] {view.Card.cardName} già selezionata"); return; }

        if (activeCard != null) activeCard.Deselect();
        activeCard = view;
        activeCard.Select();

        if (pathPreview != null) pathPreview.SetCard(activeCard.Card);

        Debug.Log($"[Player] Seleziono {activeCard.Card.cardName} (costo {activeCard.Card.cost})");
    }

    IEnumerator HandleTileClick(Tile target)
    {
        if (activeCard == null || activeUnit == null) yield break;
        if (isSwitching || isResolvingCard) yield break;

        isResolvingCard = true;

        // Snapshot: da qui in poi activeUnit può cambiare (switch in parallelo),
        // quindi NON rileggere i campi. Usa solo variabili locali.
        var user = activeUnit;
        var card = activeCard;

        // Verifica energia
        if (!Energy.CanSpend(card.Card.cost))
        {
            Debug.Log("[Player] Energia insufficiente");
            card.Shake();
            isResolvingCard = false;
            yield break;
        }

        var ctx = new CardContext
        {
            User = user,
            TargetTile = target,
            Energy = Energy
        };

        bool valid = card.Card.IsValidTarget(ctx);
        Debug.Log($"[Player] Tile {target.Coord} con {card.Card.cardName}. Valido = {valid}");

        if (!valid)
        {
            card.Shake();
            isResolvingCard = false;
            yield break;
        }

        Energy.Spend(card.Card.cost);
        if (pathPreview != null) pathPreview.ClearCard();

        activeCard = null;
        card.Deselect();

        yield return card.Card.Play(ctx);

        user.Deck.Discard(card.Card);
        var hand = handView.GetOrCreate(user);   // ← usa lo snapshot, non activeUnit
        yield return hand.AnimateDiscard(card);

        isResolvingCard = false;
    }

    IEnumerator WaitForAction()
    {
        hasPending = false;

        void OnCard(CardView v) { if (!hasPending) { pending = PlayerAction.MakeCard(v); hasPending = true; } }
        void OnTile(Tile t) { if (!hasPending) { pending = PlayerAction.MakeTile(t); hasPending = true; } }
        void OnEnd() { if (!hasPending) { pending = PlayerAction.MakeEndTurn(); hasPending = true; } }

        handView.CardClicked += OnCard;
        picker.TileClicked += OnTile;
        TurnManager.Instance.EndTurnRequested += OnEnd;

        while (!hasPending) yield return null;

        handView.CardClicked -= OnCard;
        picker.TileClicked -= OnTile;
        TurnManager.Instance.EndTurnRequested -= OnEnd;
    }
    void HandleCardsDrawn(Unit u, List<CardData> cards)
    {
        var hand = handView.GetOrCreate(u);

        foreach (var c in cards)
            hand.SpawnAt(c, Vector2.down * 600f);

        // Se l'unità è quella attiva, anima subito; altrimenti le carte
        // resteranno pronte e verranno mostrate quando si switcha.
        if (u == activeUnit)
            StartCoroutine(hand.AnimateAllToHome());
        else
            hand.HideInstant(); // mantiene le nuove carte pronte ma invisibili
    }

}