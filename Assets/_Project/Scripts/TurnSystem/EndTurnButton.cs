using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EndTurnButton : MonoBehaviour
{
    public System.Action Clicked;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    void OnClick()
    {
        Debug.Log("[EndTurn] Click");
        Clicked?.Invoke();
    }
}