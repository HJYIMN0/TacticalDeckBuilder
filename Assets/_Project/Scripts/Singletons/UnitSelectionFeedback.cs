using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class UnitSelectionFeedback : GenericSingleton<UnitSelectionFeedback>
{
    [SerializeField] CinemachineCamera focusCamera;
    [SerializeField] Transform selectionMarker;
    [SerializeField] float markerSize = 1f;
    [SerializeField] float markerHeight = 0.05f;
    [SerializeField] float markerPopDuration = 0.25f;
    [SerializeField] float markerPulseScale = 1.1f;
    [SerializeField] float markerPulseDuration = 0.6f;

    Unit current;
    Tween markerTween;

    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;

    public void Select(Unit unit)
    {
        if (unit == null || unit == current) return;
        current = unit;

        if (focusCamera != null)
        {
            focusCamera.Follow = unit.transform;
            focusCamera.gameObject.SetActive(true);
        }

        if (selectionMarker != null)
        {
            markerTween?.Kill();
            selectionMarker.position = unit.transform.position + Vector3.up * markerHeight;
            selectionMarker.localScale = Vector3.zero;
            selectionMarker.gameObject.SetActive(true);
            markerTween = selectionMarker.DOScale(markerSize, markerPopDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(StartMarkerPulse);
        }

        if (unit.Feedback != null) unit.Feedback.PlaySelect();
    }

    public void Deselect()
    {
        if (current == null && (selectionMarker == null || !selectionMarker.gameObject.activeSelf)) return;
        current = null;

        if (focusCamera != null) focusCamera.gameObject.SetActive(false);

        if (selectionMarker != null)
        {
            markerTween?.Kill();
            markerTween = selectionMarker.DOScale(0f, 0.15f)
                .SetEase(Ease.InBack)
                .OnComplete(() => selectionMarker.gameObject.SetActive(false));
        }
    }

    void StartMarkerPulse()
    {
        markerTween = selectionMarker.DOScale(markerSize * markerPulseScale, markerPulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void LateUpdate()
    {
        if (current == null) return;

        // Unit morta/disattivata mentre è selezionata: chiudi il feedback.
        if (!current.IsAlive || !current.gameObject.activeInHierarchy)
        {
            Deselect();
            return;
        }

        selectionMarker.position = current.transform.position + Vector3.up * markerHeight;
    }
}