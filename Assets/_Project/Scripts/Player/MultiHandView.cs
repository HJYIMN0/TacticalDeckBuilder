using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiHandView : MonoBehaviour
{
    [SerializeField] HandView handViewPrefab;
    [SerializeField] RectTransform container;

    readonly Dictionary<Unit, HandView> hands = new();
    Unit activeUnit;

    public event Action<CardView> CardClicked;

    public HandView Active =>
        activeUnit != null && hands.TryGetValue(activeUnit, out var h) ? h : null;

    public HandView GetOrCreate(Unit u)
    {
        if (hands.TryGetValue(u, out var existing) && existing != null) return existing;

        var h = Instantiate(handViewPrefab, container);
        h.gameObject.name = $"Hand_{u.name}";
        h.CardClicked += v => { if (u == activeUnit) CardClicked?.Invoke(v); };
        hands[u] = h;
        return h;
    }

    public void SetActive(Unit u)
    {
        activeUnit = u;
        foreach (var kv in hands)
        {
            if (kv.Value == null) continue;
            bool show = kv.Key == u;
            kv.Value.gameObject.SetActive(show);
            if (show) kv.Value.ResetVisual();   // <-- vedi Patch 3
        }
    }

    public IEnumerator SetActiveAnimated(Unit u)
    {
        if (u == activeUnit) yield break;

        // 1) nascondi la vecchia mano (se attiva)
        if (activeUnit != null
            && hands.TryGetValue(activeUnit, out var oldHand)
            && oldHand != null
            && oldHand.gameObject.activeSelf)
        {
            yield return oldHand.AnimateHide();
        }

        // 2) aggiorna riferimento attivo
        activeUnit = u;

        // 3) spegni QUALSIASI altra mano rimasta accesa
        foreach (var kv in hands)
        {
            if (kv.Key == u || kv.Value == null) continue;
            if (kv.Value.gameObject.activeSelf)
                kv.Value.gameObject.SetActive(false);
        }

        // 4) mostra la nuova mano
        if (hands.TryGetValue(u, out var newHand) && newHand != null)
            yield return newHand.AnimateShow();
    }

    public void ClearAll()
    {
        foreach (var kv in hands)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        hands.Clear();
        activeUnit = null;
    }
    /// <summary>
    /// Nasconde TUTTE le mani istantaneamente (nessuna animazione).
    /// Usato subito dopo lo spawn iniziale per evitare che più mani appaiano insieme.
    /// </summary>

    public void HideAll()
    {
        foreach (var kv in hands)
        {
            if (kv.Value == null) continue;
            kv.Value.HideInstant();
        }
        activeUnit = null;
    }

    /// <summary>
    ///  Rende attiva l'unità indicata e mostra la sua mano con l'animazione di reveal.
    ///  Le altre mani restano nascoste.</summary>
    /// <param name="u"></param>
    /// <returns></returns>

    public IEnumerator ShowHand(Unit u)
    {
        if (!hands.TryGetValue(u, out var hand) || hand == null) yield break;

        // spegni ogni altra mano ancora accesa (sicurezza)
        foreach (var kv in hands)
        {
            if (kv.Key == u || kv.Value == null) continue;
            if (kv.Value.gameObject.activeSelf)
                kv.Value.gameObject.SetActive(false);
        }

        activeUnit = u;
        yield return hand.AnimateShow();
    }
}