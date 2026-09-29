using System.Collections;
using UnityEngine;

public abstract class CardData : ScriptableObject
{
    public string cardName;
    public int cost = 1;
    public Sprite artwork;   // NUOVO
    public abstract bool IsValidTarget(CardContext ctx);
    public abstract IEnumerator Play(CardContext ctx);
}

public struct CardContext
{
    public Unit User;
    public Tile TargetTile;
    public Unit TargetUnit => TargetTile != null ? TargetTile.Occupant : null;
}