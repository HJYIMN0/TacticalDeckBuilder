using UnityEngine;

public class PlayerTurnState
{
    public PlayerTurnPhase Phase;
    public Unit ActiveUnit;     // sempre valorizzata tranne a fine turno
    public CardView Card;
    public Tile TargetTile;

    /// <summary>Reset della selezione carta, mantiene l'unità attiva.</summary>
    public void Reset()
    {
        Phase = PlayerTurnPhase.Idle;
        Card = null;
        TargetTile = null;
    }

    /// <summary>Reset totale, usato a fine turno.</summary>
    public void ResetAll()
    {
        Reset();
        ActiveUnit = null;
    }
}