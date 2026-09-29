using System.Collections;
using UnityEngine;

public abstract class AbstractUnitController : MonoBehaviour
{
    public abstract IEnumerator TakeTurn(Unit unit);
}