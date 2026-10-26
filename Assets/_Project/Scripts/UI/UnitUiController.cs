using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitUiController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthBar;

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        healthText.text = $"{currentHealth} / {maxHealth}";
        healthBar.fillAmount = (float)currentHealth / maxHealth;
    }
}
