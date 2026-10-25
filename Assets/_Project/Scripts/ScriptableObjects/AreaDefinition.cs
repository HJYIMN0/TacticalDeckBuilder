using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Tiles/Area", fileName = "Area_")]
public class AreaDefinition : ScriptableObject
{
    public string areaName;

    [Header("Bonus al faction che controlla l'area")]
    [Tooltip("Energia extra per turno")]
    public int bonusEnergy = 0;

    [Tooltip("Carte aggiunte al deck del faction quando controlla l'area")]
    public List<CardData> bonusCards = new List<CardData>();


    [Tooltip("True: le bonusCards vanno a TUTTE le unità della fazione. " +
             "False: solo al conquistatore (unità che ha triggerato la conquista)")]
    public bool isCardForAllFaction = true;
}