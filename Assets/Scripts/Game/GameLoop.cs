using System.Collections;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
    [SerializeField] private GameState[] _states;
    [SerializeField] private int _gameRounds = 8;
    public int Round { get; private set; }

    private void Start()
    {
        // StartCoroutine(GameLoopCoroutine());
    }

    public void StartGame()
    {
        StartCoroutine(GameLoopCoroutine());
    }

    private IEnumerator GameLoopCoroutine()
    {
        for ( int i = 1 ; i <= _gameRounds ; i++ )
        {
            Round = i;
            foreach (GameState state in _states)
            {
                yield return state.State();
            }
        }

        // show leaderboard and exit
    }
}
