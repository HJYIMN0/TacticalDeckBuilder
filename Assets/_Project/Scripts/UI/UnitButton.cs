using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text label;
    [SerializeField] Image portrait;
    [SerializeField] GameObject highlight;

    public event Action<Unit> Clicked;
    public Unit Unit { get; private set; }

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(() => Clicked?.Invoke(Unit));
    }

    public void Bind(Unit u)
    {
        Unit = u;
        if (label != null) label.text = u.name;
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }
}