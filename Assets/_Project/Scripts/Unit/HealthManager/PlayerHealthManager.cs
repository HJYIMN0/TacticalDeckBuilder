using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealthManager : AbstractHealthManager
{
    protected override void Die()
    {
        Debug.Log($"[Health] {name} muore");

        Unit unit = GetComponent<Unit>();

        // Libera la tile
        if (unit != null && unit.Mover != null && unit.Mover.CurrentTile != null)
            unit.Mover.CurrentTile.SetOccupant(null);

        // Deregistra dal TurnManager
        if (unit != null && TurnManager.HasInstance)
            TurnManager.Instance.Unregister(unit);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
