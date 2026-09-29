using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HandView : MonoBehaviour
{
    [SerializeField] RectTransform handRoot;
    [SerializeField] CardView cardPrefab;
    [SerializeField] float cardSpacing = 200f;
    [SerializeField] float drawDuration = 0.4f;
    [SerializeField] float discardDuration = 0.35f;
    [SerializeField] Vector2 discardOffset = new Vector2(0f, -600f);

    readonly List<CardView> views = new List<CardView>();

    public event Action<CardView> CardClicked;
    public IReadOnlyList<CardView> Views => views;

    public CardView SpawnAt(CardData card, Vector2 startOffset)
    {
        CardView view = Instantiate(cardPrefab, handRoot);
        view.OnClicked += HandleClicked;
        views.Add(view);
        view.Bind(card);

        RectTransform rt = view.GetComponent<RectTransform>();
        Vector2 home = GetSlotPosition(views.Count - 1);
        rt.anchoredPosition = home + startOffset;
        rt.localScale = Vector3.one * 0.6f;
        view.SetHome(home);
        return view;
    }

    public IEnumerator AnimateAllToHome()
    {
        int count = views.Count;
        for (int i = 0; i < count; i++)
        {
            CardView v = views[i];
            RectTransform rt = v.GetComponent<RectTransform>();
            Vector2 home = GetSlotPosition(i);
            v.SetHome(home);
            rt.DOAnchorPos(home, drawDuration).SetEase(Ease.OutBack);
            rt.DOScale(1f, drawDuration).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(drawDuration);
    }

    public IEnumerator AnimateDiscard(CardView view)
    {
        view.OnClicked -= HandleClicked;
        views.Remove(view);

        RectTransform rt = view.GetComponent<RectTransform>();
        rt.DOAnchorPos(rt.anchoredPosition + discardOffset, discardDuration).SetEase(Ease.InBack);
        rt.DOScale(0.3f, discardDuration);

        yield return new WaitForSeconds(discardDuration);

        Destroy(view.gameObject);
        Relayout();
    }

    public void Relayout()
    {
        for (int i = 0; i < views.Count; i++)
            views[i].SetHome(GetSlotPosition(i));
    }

    Vector2 GetSlotPosition(int index)
    {
        int count = views.Count;
        if (count <= 1) return Vector2.zero;
        float total = (count - 1) * cardSpacing;
        float x = -total / 2f + index * cardSpacing;
        return new Vector2(x, 0f);
    }

    void HandleClicked(CardView view) => CardClicked?.Invoke(view);
}