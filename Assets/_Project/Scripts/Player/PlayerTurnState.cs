using UnityEngine;

/// <summary>
/// Stato unico della selezione in corso. Nessuna copia in HandView/UI.
/// </summary>
public class PlayerTurnState
{
    public PlayerTurnPhase Phase;
    public CardView Card;
    public Unit Executor;
    public Unit TargetUnit;
    public Tile TargetTile;

    public void Reset()
    {
        Phase = PlayerTurnPhase.Idle;
        Card = null;
        Executor = null;
        TargetUnit = null;
        TargetTile = null;
    }
}