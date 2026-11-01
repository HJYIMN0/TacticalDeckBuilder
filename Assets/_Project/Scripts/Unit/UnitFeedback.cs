using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class UnitFeedback : MonoBehaviour
{
    [SerializeField] Transform visual;
    [SerializeField] CinemachineImpulseSource impulseSource;   // opzionale

    [Header("Attacco")]
    [SerializeField] Vector3 anticipationScale = new Vector3(1.15f, 0.8f, 1.15f);
    [SerializeField] float anticipationDuration = 0.12f;
    [SerializeField] float lungeDistance = 0.4f;
    [SerializeField] float lungeDuration = 0.08f;
    [SerializeField] float recoverDuration = 0.18f;
    [SerializeField] bool faceTargetOnAttack = true;

    [Header("Colpito")]
    [SerializeField] Vector3 hitSquash = new Vector3(1.2f, 0.8f, 1.2f);
    [SerializeField] float hitRecoilDistance = 0.25f;
    [SerializeField] float hitRecoilDuration = 0.06f;
    [SerializeField] float hitRecoverDuration = 0.2f;

    [Header("Selezione / atterraggio")]
    [SerializeField] Vector3 selectPunch = new Vector3(0.12f, -0.12f, 0.12f);
    [SerializeField] Vector3 landPunch = new Vector3(0.1f, -0.12f, 0.1f);

    Vector3 baseScale;
    Tween active;

    void Awake()
    {
        if (visual == null) visual = transform;
        baseScale = visual.localScale;
    }

    /// <summary>Si ferma al momento dell'impatto: da lì il chiamante applica il danno. Il rientro continua in background.</summary>
    public IEnumerator PlayAttack(Vector3 targetWorldPos)
    {
        CompleteActive();

        Vector3 start = visual.position;
        Vector3 dir = targetWorldPos - start;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        // Anticipazione: squash + piccolo passo indietro
        seq.Append(visual.DOScale(Vector3.Scale(baseScale, anticipationScale), anticipationDuration).SetEase(Ease.OutQuad));
        seq.Join(visual.DOMove(start - dir * (lungeDistance * 0.3f), anticipationDuration).SetEase(Ease.OutQuad));
        if (faceTargetOnAttack && dir != Vector3.zero)
            seq.Join(visual.DORotateQuaternion(Quaternion.LookRotation(dir), anticipationDuration));
        // Affondo: stretch
        seq.Append(visual.DOMove(start + dir * lungeDistance, lungeDuration).SetEase(Ease.InQuad));
        seq.Join(visual.DOScale(baseScale * 1.1f, lungeDuration));
        // Rientro
        seq.Append(visual.DOMove(start, recoverDuration).SetEase(Ease.OutQuad));
        seq.Join(visual.DOScale(baseScale, recoverDuration).SetEase(Ease.OutBack));
        active = seq;

        yield return seq.WaitForPosition(anticipationDuration + lungeDuration);
    }

    public void PlayHit(Vector3 attackerWorldPos)
    {
        CompleteActive();

        Vector3 start = visual.position;
        Vector3 away = start - attackerWorldPos;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.zero;

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(visual.DOMove(start + away * hitRecoilDistance, hitRecoilDuration).SetEase(Ease.OutQuad));
        seq.Join(visual.DOScale(Vector3.Scale(baseScale, hitSquash), hitRecoilDuration));
        seq.Append(visual.DOMove(start, hitRecoverDuration).SetEase(Ease.OutBack));
        seq.Join(visual.DOScale(baseScale, hitRecoverDuration).SetEase(Ease.OutBack));
        active = seq;

        if (impulseSource != null) impulseSource.GenerateImpulse();
    }

    public void PlaySelect()
    {
        CompleteActive();
        active = visual.DOPunchScale(selectPunch, 0.25f, 6, 0.6f).SetLink(gameObject);
    }

    public void PlayLand()
    {
        CompleteActive();
        active = visual.DOPunchScale(landPunch, 0.18f, 1, 0f).SetLink(gameObject);
    }

    /// <summary>Da usare con yield return: attende la fine dell'animazione in corso (o niente).</summary>
    public YieldInstruction WaitForIdle()
        => active != null && active.IsActive() ? active.WaitForCompletion() : null;

    void CompleteActive()
    {
        if (active != null && active.IsActive()) active.Kill(true);   // true = completa, così scala/posizione tornano allo stato finale
        active = null;
    }
}