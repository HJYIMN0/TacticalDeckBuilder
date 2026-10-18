using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitSelectorUI : MonoBehaviour
{
    [SerializeField] RectTransform container;
    [SerializeField] UnitButton buttonPrefab;

    public event Action<Unit> UnitSelected;

    readonly List<UnitButton> buttons = new();

    /// <summary>
    /// Mostra solo le unità che passano il filtro (esecutori o target validi).
    /// </summary>
    public void ShowAll(IEnumerable<Unit> units, Func<Unit, bool> validFilter)
    {
        Clear();
        foreach (var u in units)
        {
            if (validFilter != null && !validFilter(u)) continue;

            var b = Instantiate(buttonPrefab, container);
            b.Bind(u);
            var captured = u;
            b.Clicked += _ => UnitSelected?.Invoke(captured);
            buttons.Add(b);
        }
    }

    public void Clear()
    {
        foreach (var b in buttons) if (b != null) Destroy(b.gameObject);
        buttons.Clear();
    }

    public void SetActive(Unit unit)
    {
        foreach (var b in buttons)
            if (b != null) b.SetHighlight(b.Unit == unit);
    }
}