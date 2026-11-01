using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image artwork;
    [SerializeField] TMP_Text label;
    [SerializeField] private TextMeshProUGUI cardCostText;
    [SerializeField] private TextMeshProUGUI cardDescriptionText;
    [SerializeField] RectTransform rect;

    [Header("Hover")]
    [SerializeField] float hoverLift = 90f;
    [SerializeField] float hoverScale = 1.25f;
    [SerializeField] float hoverDuration = 0.15f;

    [Header("Selezione")]
    [SerializeField] float selectedLift = 140f;
    [SerializeField] float selectedScale = 1.35f;

    [Header("Dim (quando un'altra carta è selezionata)")]
    [SerializeField] CanvasGroup dimGroup;
    [SerializeField, Range(0f, 1f)] float dimmedAlpha = 0.55f;
    [SerializeField] float dimDuration = 0.2f;

    // --- Stato statico di selezione (una sola carta selezionata alla volta) ---
    public static CardView CurrentSelected { get; private set; }
    public static event Action<CardView> onCardSelected;
    public static event Action<CardView> onCardDeselected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        CurrentSelected = null;
        onCardSelected = null;
        onCardDeselected = null;
    }

    public RectTransform Rect => rect;
    public Vector2 HomePosition => homePosition;
    public float HomeRotation => homeRotation;
    public CardData Card { get; private set; }
    public System.Action<CardView> OnClicked;

    Vector2 homePosition;
    float homeRotation;
    bool selected;
    bool raised;
    int siblingBeforeRaise;

    void Awake()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (dimGroup == null) dimGroup = GetComponent<CanvasGroup>();
        if (dimGroup == null) dimGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        onCardSelected += HandleAnySelected;
        onCardDeselected += HandleAnyDeselected;
        // Una mano riattivata mentre c'è una selezione attiva deve nascere già dimmata.
        ApplyDim(CurrentSelected != null && CurrentSelected != this, true);
    }

    void OnDisable()
    {
        onCardSelected -= HandleAnySelected;
        onCardDeselected -= HandleAnyDeselected;
    }

    void OnDestroy()
    {
        if (rect != null) rect.DOKill();
        if (dimGroup != null) dimGroup.DOKill();

        // Carta giocata/scartata mentre era selezionata: sblocca il dim delle altre.
        if (CurrentSelected == this)
        {
            CurrentSelected = null;
            onCardDeselected?.Invoke(this);
        }
    }

    public void Bind(CardData card)
    {
        Card = card;
        if (label != null) label.text = card.cardName;
        if (artwork != null && card.artwork != null) artwork.sprite = card.artwork;
        if (cardCostText != null) cardCostText.text = $"Cost: {card.cost}";
        if (cardDescriptionText != null) cardDescriptionText.text = card.Description;
    }

    public void SetHome(Vector2 pos, float rotZ = 0f)
    {
        homePosition = pos;
        homeRotation = rotZ;
    }

    public void SetHomeInstant(Vector2 pos, float rotZ = 0f)
    {
        homePosition = pos;
        homeRotation = rotZ;
        if (rect != null)
        {
            rect.anchoredPosition = pos;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }
    }

    public void Select()
    {
        if (selected) return;
        selected = true;

        BringToFront();
        rect.DOKill();
        rect.DOAnchorPos(homePosition + Vector2.up * selectedLift, hoverDuration).SetEase(Ease.OutBack);
        rect.DOScale(selectedScale, hoverDuration).SetEase(Ease.OutBack);
        rect.DOLocalRotateQuaternion(Quaternion.identity, hoverDuration).SetEase(Ease.OutQuad);

        CurrentSelected = this;
        onCardSelected?.Invoke(this);
    }

    public void Deselect()
    {
        if (!selected) return;
        selected = false;

        ReturnHome(0.15f);
        RestoreOrder();

        if (CurrentSelected == this) CurrentSelected = null;
        onCardDeselected?.Invoke(this);
    }

    public void Shake()
    {
        rect.DOShakeAnchorPos(0.25f, 20f, 20, 90, false, true);
    }

    public void KillTweens()
    {
        if (rect != null) rect.DOKill();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Durante una selezione le altre carte non reagiscono all'hover.
        if (selected || CurrentSelected != null) return;

        BringToFront();
        rect.DOKill();
        rect.DOAnchorPos(homePosition + Vector2.up * hoverLift, hoverDuration).SetEase(Ease.OutQuad);
        rect.DOScale(hoverScale, hoverDuration).SetEase(Ease.OutQuad);
        rect.DOLocalRotateQuaternion(Quaternion.identity, hoverDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selected) return;

        ReturnHome(hoverDuration);
        RestoreOrder();
    }

    public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(this);

    void ReturnHome(float duration)
    {
        rect.DOKill();
        rect.DOAnchorPos(homePosition, duration).SetEase(Ease.OutQuad);
        rect.DOScale(1f, duration).SetEase(Ease.OutQuad);
        rect.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, homeRotation), duration).SetEase(Ease.OutQuad);
    }

    void BringToFront()
    {
        if (raised) return;
        raised = true;
        siblingBeforeRaise = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
    }

    void RestoreOrder()
    {
        if (!raised) return;
        raised = false;
        transform.SetSiblingIndex(siblingBeforeRaise);
    }

    void HandleAnySelected(CardView other)
    {
        if (other != this) ApplyDim(true, false);
    }

    void HandleAnyDeselected(CardView other) => ApplyDim(false, false);

    void ApplyDim(bool dim, bool instant)
    {
        if (dimGroup == null) return;
        float a = dim ? dimmedAlpha : 1f;
        dimGroup.DOKill();
        if (instant) dimGroup.alpha = a;
        else dimGroup.DOFade(a, dimDuration);
    }
}