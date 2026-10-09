using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitSelectorUI : MonoBehaviour
{
    [SerializeField] RectTransform container;
    [SerializeField] UnitButton buttonPrefab;

    public event Action<Unit> UnitSelected;

    readonly List<UnitButton> buttons = new();

    public void Build(IEnumerable<Unit> units, Unit active)
    {
        Clear();
        foreach (var u in units)
        {
            var b = Instantiate(buttonPrefab, container);
            b.Bind(u);
            var captured = u;
            b.Clicked += _ => UnitSelected?.Invoke(captured);
            buttons.Add(b);
        }
        SetActive(active);
    }

    public void SetActive(Unit active)
    {
        foreach (var b in buttons)
            b.SetHighlight(b.Unit == active);
    }

    public void Clear()
    {
        foreach (var b in buttons) if (b != null) Destroy(b.gameObject);
        buttons.Clear();
    }
}