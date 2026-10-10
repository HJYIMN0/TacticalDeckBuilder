using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    readonly List<CardData> drawPile = new List<CardData>();
    readonly List<CardData> hand = new List<CardData>();
    readonly List<CardData> discardPile = new List<CardData>();

    public IReadOnlyList<CardData> Hand => hand;

    public Deck(IEnumerable<CardData> initial)
    {
        drawPile.AddRange(initial);
        Shuffle(drawPile);
    }

    public void Draw(int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    Debug.Log("[Deck] Pila vuota e scarto vuoto: niente da pescare");
                    return;
                }
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
                Debug.Log($"[Deck] Rimescolo: {drawPile.Count} carte");
            }
            hand.Add(drawPile[0]);
            drawPile.RemoveAt(0);
        }
        Debug.Log($"[Deck] Draw -> mano {hand.Count}, drawPile {drawPile.Count}, scarto {discardPile.Count}");
    }

    public void Discard(CardData card)
    {
        if (hand.Remove(card))
        {
            discardPile.Add(card);
            Debug.Log($"[Deck] Scartata {card.cardName} -> scarto {discardPile.Count}");
        }
    }

    public void DiscardHand()
    {
        discardPile.AddRange(hand);
        hand.Clear();
    }

    static void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // CLASSE: Deck
    // Aggiungi questo metodo

    public void AddBonusCards(IEnumerable<CardData> cards)
    {
        foreach (var c in cards)
            if (c != null)
                drawPile.Add(c);   // oppure hand, se preferisci

        Shuffle(drawPile);
    }
}