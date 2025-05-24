using System.Collections;
using UnityEngine;

public abstract class GameState : MonoBehaviour
{
    public abstract IEnumerator State();
}