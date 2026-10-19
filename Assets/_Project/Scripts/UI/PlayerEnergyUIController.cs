using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerEnergyUIController : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerFaction faction;   // la stessa usata da PlayerController

    [Header("UI")]
    [SerializeField] private Image energyBar;   // Image con Fill = Horizontal
    [SerializeField] private TextMeshProUGUI energyText;   // Text con "current / max"

    private IEnergyPool pool;

    private void Start()
    {
        pool = faction.Energy;
        pool.Changed += UpdateUI;
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (pool != null) pool.Changed -= UpdateUI;
    }

    private void UpdateUI()
    {
        int max = pool.EnergyPerTurn;
        int current = pool.Current;

        int clamped = Mathf.Clamp(current, 0, max);
        float fill = max > 0 ? (float)clamped / max : 0f;

        Debug.Log($"[PlayerEnergyUIController] UpdateUI current={current} max={max} fill={fill}");

        energyBar.fillAmount = fill;
        energyText.text = $"{clamped} / {max}";
    }
}