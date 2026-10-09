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

    /// <summary>Switch istantaneo (nessuna animazione). Utile per setup iniziale.</summary>
    public void SetActive(Unit u)
    {
        activeUnit = u;
        foreach (var kv in hands)
            if (kv.Value != null)
                kv.Value.gameObject.SetActive(kv.Key == u);
    }

    /// <summary>Switch animato: hide della vecchia mano + show della nuova.</summary>
    public IEnumerator SetActiveAnimated(Unit u)
    {
        if (u == activeUnit) yield break;

        // 1) nascondi la vecchia mano
        if (activeUnit != null
            && hands.TryGetValue(activeUnit, out var oldHand)
            && oldHand != null
            && oldHand.gameObject.activeSelf)
        {
            yield return oldHand.AnimateHide();
        }

        // 2) aggiorna riferimento attivo
        activeUnit = u;

        // 3) sicurezza: spegni eventuali altre mani rimaste accese
        foreach (var kv in hands)
            if (kv.Key != u && kv.Value != null && kv.Value.gameObject.activeSelf)
                kv.Value.gameObject.SetActive(false);

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
}