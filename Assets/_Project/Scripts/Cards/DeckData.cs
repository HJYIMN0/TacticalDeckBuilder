using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Deck")]
public class DeckData : ScriptableObject
{
    public List<CardData> cards = new List<CardData>();
}