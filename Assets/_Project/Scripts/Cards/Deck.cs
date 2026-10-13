// FILE: Assets/_Project/Scripts/Cards/Deck.cs
// CLASSE: Deck

using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    readonly List<CardData> drawPile = new();
    readonly List<CardData> hand = new();
    readonly List<CardData> discardPile = new();
    readonly List<CardData> removedPile = new();   // carte uniche uscite dal gioco

    public IReadOnlyList<CardData> Hand => hand;
    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;
    public int RemovedPileCount => removedPile.Count;

    public Deck(IEnumerable<CardData> initial)
    {
        if (initial != null) drawPile.AddRange(initial);
        Shuffle(drawPile);
    }

    // --- Pescata ---

    /// <summary>Pesca esattamente n carte se possibile; rimescola lo scarto se serve.</summary>
    public void Draw(int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (drawPile.Count == 0 && !TryReshuffleDiscard())
                break;
            hand.Add(drawPile[0]);
            drawPile.RemoveAt(0);
        }
        Debug.Log($"[Deck] Draw({n}) -> mano {hand.Count}, mazzo {drawPile.Count}, scarto {discardPile.Count}, rimosse {removedPile.Count}");
    }

    /// <summary>
    /// Pesca finché la mano non raggiunge `target` carte.
    /// Utile quando una pescata parziale ha lasciato la mano incompleta.
    /// </summary>
    public void DrawUpTo(int target)
    {
        while (hand.Count < target)
        {
            if (drawPile.Count == 0 && !TryReshuffleDiscard())
                break;
            hand.Add(drawPile[0]);
            drawPile.RemoveAt(0);
        }
    }

    // --- Scarto ---

    public void Discard(CardData card)
    {
        if (card == null) return;
        if (!hand.Remove(card)) return;

        if (card.isUnique)
        {
            removedPile.Add(card);
            Debug.Log($"[Deck] {card.cardName} è UNICA: rimossa dal gioco");
        }
        else
        {
            discardPile.Add(card);
            Debug.Log($"[Deck] Scartata {card.cardName} -> scarto {discardPile.Count}");
        }
    }

    public void DiscardHand()
    {
        var snapshot = new List<CardData>(hand);
        foreach (var c in snapshot) Discard(c);
    }

    // --- Interni ---

    bool TryReshuffleDiscard()
    {
        if (discardPile.Count == 0) return false;
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
        Debug.Log($"[Deck] Rimescolo: {drawPile.Count} carte");
        return true;
    }

    static void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void AddToDrawPile(IEnumerable<CardData> cards)
    {
        if (cards == null) return;
        int added = 0;
        foreach (var c in cards)
        {
            if (c == null) continue;
            drawPile.Add(c);
            added++;
        }
        if (added > 0)
        {
            Shuffle(drawPile);
            Debug.Log($"[Deck] Aggiunte {added} carte al mazzo (totale {drawPile.Count})");
        }
    }
}