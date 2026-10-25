using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image artwork;
    [SerializeField] TMP_Text label;
    [SerializeField] private TextMeshProUGUI cardCostText;
    [SerializeField] private TextMeshProUGUI cardDescriptionText;
    [SerializeField] RectTransform rect;
    public RectTransform Rect => rect;
    public Vector2 HomePosition => homePosition;

    public CardData Card { get; private set; }
    public System.Action<CardView> OnClicked;

    Vector2 homePosition;
    bool selected;

    void Awake()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
    }

    public void Bind(CardData card)
    {
        Card = card;
        if (label != null) label.text = card.cardName;
        if (artwork != null && card.artwork != null) artwork.sprite = card.artwork;
        if (cardCostText != null) cardCostText.text = $"Cost: {card.cost}";
        if (cardDescriptionText != null) cardDescriptionText.text = card.Description;
    }

    public void SetHome(Vector2 pos) => homePosition = pos;

    public void Select()
    {
        selected = true;
        rect.DOAnchorPos(homePosition + new Vector2(0f, 60f), 0.15f).SetEase(Ease.OutBack);
        rect.DOScale(1.1f, 0.15f);
    }

    public void Deselect()
    {
        selected = false;
        rect.DOAnchorPos(homePosition, 0.15f).SetEase(Ease.OutQuad);
        rect.DOScale(1f, 0.15f);
    }

    public void Shake()
    {
        rect.DOShakeAnchorPos(0.25f, 20f, 20, 90, false, true);
    }


    public void KillTweens()
    {
        if (rect != null) rect.DOKill();
    }

    public void SetHomeInstant(Vector2 pos)
    {
        homePosition = pos;
        if (rect != null) rect.anchoredPosition = pos;
    }
    public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(this);
}