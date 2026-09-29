using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugCardTester : MonoBehaviour
{
    [SerializeField] Unit unit;
    [SerializeField] CardData card;
    [SerializeField] Tile targetTile;

    bool busy;

    void Update()
    {
        if (busy) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;
        StartCoroutine(PlayOnce());
    }

    IEnumerator PlayOnce()
    {
        if (unit == null || card == null || targetTile == null)
        {
            Debug.LogWarning("[DebugCardTester] Riferimenti mancanti");
            yield break;
        }

        busy = true;
        var ctx = new CardContext { User = unit, TargetTile = targetTile };

        if (!card.IsValidTarget(ctx))
        {
            Debug.Log($"[DebugCardTester] {card.cardName} non valida su {targetTile.Coord}");
            busy = false;
            yield break;
        }

        Debug.Log($"[DebugCardTester] Gioco {card.cardName} verso {targetTile.Coord}");
        yield return card.Play(ctx);
        Debug.Log("[DebugCardTester] Fine");
        busy = false;
    }
}