using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tiles/Area", fileName = "Area_")]
public class AreaDefinition : ScriptableObject
{
    public string areaName;

    [Header("Bonus al faction che controlla l'area")]
    [Tooltip("Energia extra per turno")]
    public int bonusEnergy = 0;

    [Tooltip("Carte aggiunte al deck del faction quando controlla l'area")]
    public List<CardData> bonusCards = new List<CardData>();
}