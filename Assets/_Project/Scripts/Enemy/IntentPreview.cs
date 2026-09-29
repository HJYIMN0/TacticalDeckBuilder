using System.Collections.Generic;
using UnityEngine;

public class IntentPreview : MonoBehaviour
{
    [SerializeField] EnemyAiController enemy;
    [SerializeField] LineRenderer moveLine;
    [SerializeField] LineRenderer attackLine;
    [SerializeField] Color moveColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] Color attackColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] float heightOffset = 0.3f;
    [SerializeField] int hardCapPerSegment = 20;

    void Awake()
    {
        if (enemy == null) enemy = FindAnyObjectByType<EnemyAiController>();
        if (enemy == null) { Debug.LogWarning("[IntentPreview] Nessun EnemyAiController"); return; }
        enemy.OnIntentChanged += Refresh;
    }

    void Start() => Refresh();

    void OnDestroy()
    {
        if (enemy != null) enemy.OnIntentChanged -= Refresh;
    }

    void Refresh()
    {
        Unit enemyUnit = enemy != null ? enemy.GetComponent<Unit>() : null;
        if (enemyUnit == null || enemyUnit.Tile == null) { HideAll(); return; }

        var intents = new List<EnemyAiController.Intent>(enemy.PendingIntents);
        if (intents.Count == 0) { HideAll(); return; }

        // Ricostruisco la sequenza: parto dalla posizione attuale,
        // simulo ogni passo e accumulo i punti del percorso.
        var movePoints = new List<Vector3>();
        movePoints.Add(ToPoint(enemyUnit.Tile.transform.position));

        Tile simulatedPos = enemyUnit.Tile;
        Tile finalAttackTarget = null;
        Vector3 attackFrom = enemyUnit.Tile.transform.position;

        foreach (var intent in intents)
        {
            if (intent.card is MovementCard move)
            {
                if (TileManager.Instance.TryGetPath(simulatedPos, intent.target, move.maxDistance, out var path)
                    && path.Count > 1)
                {
                    // path[0] è la posizione simulata attuale: la salto perché già presente
                    for (int i = 1; i < path.Count; i++)
                        movePoints.Add(ToPoint(path[i].transform.position));

                    simulatedPos = intent.target;
                }
                else
                {
                    Debug.LogWarning($"[IntentPreview] Segmento non ricostruibile: {simulatedPos.Coord} -> {intent.target.Coord} (max {move.maxDistance})");
                }
            }
            else if (intent.card is AttackCard attack)
            {
                finalAttackTarget = intent.target;
                attackFrom = simulatedPos.transform.position;
                // Non modifico simulatedPos: l'attacco non sposta l'enemy
            }
        }

        // A. percorso di movimento
        if (movePoints.Count > 1)
            DrawPath(moveLine, movePoints, moveColor);
        else
            Hide(moveLine);

        // B. linea d'attacco dal punto in cui l'enemy sarà
        if (finalAttackTarget != null)
            DrawStraight(attackLine, attackFrom, finalAttackTarget.transform.position, attackColor);
        else
            Hide(attackLine);
    }

    void DrawPath(LineRenderer line, List<Vector3> points, Color color)
    {
        if (line == null) return;
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
        line.startColor = line.endColor = color;
    }

    void DrawStraight(LineRenderer line, Vector3 a, Vector3 b, Color color)
    {
        if (line == null) return;
        line.positionCount = 2;
        line.SetPosition(0, ToPoint(a));
        line.SetPosition(1, ToPoint(b));
        line.startColor = line.endColor = color;
    }

    void Hide(LineRenderer line) { if (line != null) line.positionCount = 0; }
    void HideAll() { Hide(moveLine); Hide(attackLine); }

    Vector3 ToPoint(Vector3 p) => new Vector3(p.x, p.y + heightOffset, p.z);
}