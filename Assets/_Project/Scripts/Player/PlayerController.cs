using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : AbstractUnitController
{
    [SerializeField] TilePicker picker;
    [SerializeField] HandView handView;
    [SerializeField] PathPreview pathPreview;

    PlayerAction pending;
    bool hasPending;

    public override IEnumerator TakeTurn(Unit unit)
    {
        if (picker == null || handView == null)
        {
            Debug.LogError("[Player] Setup mancante (picker/handView)");
            yield break;
        }
        if (!TurnManager.HasInstance)
        {
            Debug.LogError("[Player] TurnManager non disponibile");
            yield break;
        }

        Debug.Log($"[Player] === Inizio turno (drawPerTurn={unit.DrawPerTurn}) ===");

        // 1. Pesca N carte
        unit.Deck.Draw(unit.DrawPerTurn);
        List<CardData> drawn = new List<CardData>(unit.Deck.Hand);
        Debug.Log($"[Player] Pescate {drawn.Count} carte:");
        foreach (var c in drawn) Debug.Log($"  - {c.cardName} (cost {c.cost})");

        // 2. Spawn viste
        foreach (var c in drawn)
            handView.SpawnAt(c, Vector2.down * 600f);
        yield return handView.AnimateAllToHome();

        // 3. Loop principale
        CardView active = null;
        bool endTurn = false;

        while (!endTurn)
        {
            yield return WaitForAction();

            PlayerAction action = pending;
            hasPending = false;

            switch (action.Type)
            {
                case PlayerAction.Kind.EndTurn:
                    Debug.Log("[Player] End turn ricevuto");
                    if (active != null) { active.Deselect(); active = null; }
                    if (pathPreview != null) pathPreview.ClearCard();
                    endTurn = true;
                    break;

                case PlayerAction.Kind.CardClicked:
                    CardView clicked = action.CardView;
                    if (active == clicked)
                    {
                        Debug.Log($"[Player] {active.Card.cardName} già selezionata");
                        break;
                    }
                    if (active != null)
                    {
                        Debug.Log($"[Player] Cambio carta: {active.Card.cardName} -> {clicked.Card.cardName}");
                        active.Deselect();
                    }
                    else
                    {
                        Debug.Log($"[Player] Seleziono {clicked.Card.cardName}");
                    }
                    active = clicked;
                    active.Select();
                    if (pathPreview != null) pathPreview.SetCard(active.Card);
                    break;

                case PlayerAction.Kind.TileClicked:
                    Tile target = action.TileTarget;
                    if (active == null)
                    {
                        Debug.Log("[Player] Tile cliccata senza carta selezionata, ignoro");
                        break;
                    }

                    var ctx = new CardContext { User = unit, TargetTile = target };
                    bool valid = active.Card.IsValidTarget(ctx);
                    Debug.Log($"[Player] Tile {target.Coord} con {active.Card.cardName}. IsValidTarget = {valid}");

                    if (valid)
                    {
                        if (pathPreview != null) pathPreview.ClearCard();
                        Debug.Log($"[Player] Gioco {active.Card.cardName} su {target.Coord}");
                        active.Deselect();
                        yield return active.Card.Play(ctx);
                        unit.Deck.Discard(active.Card);
                        Debug.Log($"[Player] Scarto {active.Card.cardName}");
                        yield return handView.AnimateDiscard(active);
                        active = null;
                    }
                    else
                    {
                        Debug.Log("[Player] Tile non valida, riprova");
                        active.Shake();
                    }
                    break;
            }
        }

        // 4. Scarta la mano residua
        int remaining = handView.Views.Count;
        Debug.Log($"[Player] Fine turno. Scarto {remaining} carte residue.");
        var snapshot = new List<CardView>(handView.Views);
        foreach (var v in snapshot)
        {
            unit.Deck.Discard(v.Card);
            yield return handView.AnimateDiscard(v);
        }

        Debug.Log("[Player] === Fine turno ===");
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
}