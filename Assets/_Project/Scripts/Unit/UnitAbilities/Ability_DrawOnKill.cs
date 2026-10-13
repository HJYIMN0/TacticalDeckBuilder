using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Draw On Kill", fileName = "Ability_DrawOnKill")]
public class Ability_DrawOnKill : UnitAbility
{
    [SerializeField] int cardsToDraw = 1;

    public override void OnUnitKilled(Unit owner, Unit victim)
    {
        if (owner == null) return;
        Debug.Log($"[Ability] {owner.name} uccide {victim?.name}: pesca {cardsToDraw}");
        owner.DrawCards(cardsToDraw);
    }

    public override void OnObjectiveDestroyed(Unit owner, DestructibleTile objective)
    {
        if (owner == null) return;
        Debug.Log($"[Ability] {owner.name} distrugge {objective?.name}: pesca {cardsToDraw}");
        owner.DrawCards(cardsToDraw);
    }
}