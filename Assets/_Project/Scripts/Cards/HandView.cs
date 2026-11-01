using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using System;

public class HandView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform handRoot;
    [SerializeField] CardView cardPrefab;
    [Header("Ventaglio")]
    [SerializeField] float fanRadius = 1800f;     // raggio del pivot sotto la mano: più basso = arco più marcato
    [SerializeField] float fanAngleStep = 5f;     // gradi tra due carte
    [SerializeField] float maxFanAngle = 30f;     // apertura totale massima
    [SerializeField] float drawDuration = 0.4f;
    [Header("Scarto")]
    [SerializeField] float discardDuration = 0.35f;
    [SerializeField] Vector2 discardOffset = new Vector2(0f, -600f);
    
    [Header("Transition")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] float hideDuration = 0.22f;
    [SerializeField] float showDuration = 0.30f;
    [SerializeField] float stagger = 0.04f;
    [SerializeField] float offscreenOffset = 400f;

    readonly List<CardView> views = new List<CardView>();

    public event Action<CardView> CardClicked;
    public IReadOnlyList<CardView> Views => views;

    public CardView SpawnAt(CardData card, Vector2 startOffset)
    {
        CardView view = Instantiate(cardPrefab, handRoot);
        view.OnClicked += HandleClicked;
        views.Add(view);
        view.Bind(card);

        Relayout();

        RectTransform rt = view.GetComponent<RectTransform>();
        GetSlotPose(views.Count - 1, out Vector2 home, out float rot);
        rt.anchoredPosition = home + startOffset;
        rt.localRotation = Quaternion.Euler(0f, 0f, rot);
        rt.localScale = Vector3.one * 0.6f;
        view.SetHome(home, rot);
        return view;
    }

    public IEnumerator AnimateAllToHome()
    {
        int count = views.Count;
        for (int i = 0; i < count; i++)
        {
            if (i >= views.Count) break;

            CardView v = views[i];
            if (v == null) continue;

            GetSlotPose(i, out Vector2 home, out float rot);
            v.SetHome(home, rot);

            RectTransform rt = v.Rect;
            float delay = i * stagger;
            rt.DOAnchorPos(home, drawDuration).SetEase(Ease.OutBack).SetDelay(delay);
            rt.DOScale(1f, drawDuration).SetEase(Ease.OutBack).SetDelay(delay);
            rt.DOLocalRotateQuaternion(Quaternion.Euler(0f, 0f, rot), drawDuration).SetEase(Ease.OutBack).SetDelay(delay);
        }
        yield return new WaitForSeconds(drawDuration + count * stagger);
    }

    public IEnumerator AnimateDiscard(CardView view)
    {
        // view distrutta?
        if (view == null) yield break;

        // già scartata da questa mano?
        if (!views.Contains(view)) yield break;

        view.OnClicked -= HandleClicked;
        views.Remove(view);

        // usa la proprietà cached, non GetComponent (evita chiamate native su OG morti)
        RectTransform rt = view.Rect;
        rt.DOAnchorPos(rt.anchoredPosition + discardOffset, discardDuration).SetEase(Ease.InBack);
        rt.DOScale(0.3f, discardDuration);

        yield return new WaitForSeconds(discardDuration);

        if (view != null)
            Destroy(view.gameObject);

        Relayout();
    }


    void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public IEnumerator AnimateHide()
    {
        EnsureCanvasGroup();
        canvasGroup.DOKill();

        var seq = DOTween.Sequence();
        seq.Join(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));

        for (int i = 0; i < Views.Count; i++)
        {
            var v = Views[i];
            if (v == null) continue;

            v.KillTweens();
            Vector2 target = v.HomePosition + Vector2.down * offscreenOffset;

            var t = DOTween.Sequence();
            t.Join(v.Rect.DOAnchorPos(target, hideDuration).SetEase(Ease.InBack));
            t.Join(v.Rect.DOScale(0.7f, hideDuration).SetEase(Ease.InQuad));

            seq.Insert(i * stagger, t);
        }

        yield return seq.WaitForCompletion();
        gameObject.SetActive(false);
    }

    public IEnumerator AnimateShow()
    {
        gameObject.SetActive(true);
        EnsureCanvasGroup();
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;

        foreach (var v in Views)
        {
            if (v == null) continue;
            v.KillTweens();
            v.Rect.anchoredPosition = v.HomePosition + Vector2.down * offscreenOffset;
            v.Rect.localScale = Vector3.one * 0.8f;
        }

        var seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(1f, showDuration * 0.6f).SetEase(Ease.OutQuad));

        for (int i = 0; i < Views.Count; i++)
        {
            var v = Views[i];
            if (v == null) continue;

            var t = DOTween.Sequence();
            t.Join(v.Rect.DOAnchorPos(v.HomePosition, showDuration).SetEase(Ease.OutBack));
            t.Join(v.Rect.DOScale(1f, showDuration).SetEase(Ease.OutBack));

            seq.Insert(i * stagger, t);
        }

        yield return seq.WaitForCompletion();
    }

    public void ResetVisual()
    {
        EnsureCanvasGroup();
        canvasGroup.DOKill();
        canvasGroup.alpha = 1f;

        foreach (var v in Views)
        {
            if (v == null) continue;
            v.KillTweens();
            v.Rect.anchoredPosition = v.HomePosition;
            v.Rect.localRotation = Quaternion.Euler(0f, 0f, v.HomeRotation);
            v.Rect.localScale = Vector3.one;
        }
    }
    public void Relayout()
    {
        for (int i = 0; i < views.Count; i++)
        {
            if (views[i] == null) continue;
            GetSlotPose(i, out Vector2 home, out float rot);
            views[i].SetHome(home, rot);
        }
    }

    void GetSlotPose(int index, out Vector2 pos, out float rotZ)
    {
        int count = views.Count;
        if (count <= 1)
        {
            pos = Vector2.zero;
            rotZ = 0f;
            return;
        }

        float step = Mathf.Min(fanAngleStep, maxFanAngle / (count - 1));
        float angle = ((count - 1) * 0.5f - index) * step;   // carta a sinistra = angolo positivo (CCW)
        float rad = angle * Mathf.Deg2Rad;

        pos = new Vector2(-fanRadius * Mathf.Sin(rad), fanRadius * (Mathf.Cos(rad) - 1f));
        rotZ = angle;
    }

    void HandleClicked(CardView view) => CardClicked?.Invoke(view);

    /// <summary>
    /// Nasconde la mano immediatamente (nessuna animazione).
    /// Le carte restano nella loro posizione corrente, alpha a 0, GameObject disattivato.
    ///Usato dopo lo spawn iniziale per evitare che più mani appaiano insieme.
    ///</summary>


    public void HideInstant()
    {
        EnsureCanvasGroup();
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;

        foreach (var v in Views)
        {
            if (v == null) continue;
            v.KillTweens();                        // ferma tween in corso sulle carte
            v.Rect.localScale = Vector3.one;       // pulisci eventuali scale residue
        }

        gameObject.SetActive(false);
    }

}