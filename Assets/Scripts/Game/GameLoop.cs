using System.Collections;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
    [SerializeField] private GameState[] _states;
    private int _gameRounds;

    private void Start()
    {
        _gameRounds = 8;
        StartCoroutine(GameLoopCoroutine());
    }

    private IEnumerator GameLoopCoroutine()
    {
        for ( int i = 0 ; i < _gameRounds ; i++ )
        {
            foreach (GameState state in _states)
            {
                yield return state.State();
            }
        }

        // show leaderboard and exit
    }
}
