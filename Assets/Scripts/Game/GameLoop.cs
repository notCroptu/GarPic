using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameLoop : NetworkBehaviour
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
        Debug.Log("Trying to start game loop. ");

        if ( ! IsServer && ! IsHost ) return; // only host allowed to run game loop

        Debug.Log("Trying to start game loop. and is server or host. ");

        StartCoroutine(GameLoopCoroutine());
    }

    private IEnumerator GameLoopCoroutine()
    {
        for ( int i = 1 ; i <= _gameRounds ; i++ )
        {
            Round = i;
            foreach (GameState state in _states)
            {
                Debug.Log("Starting gamestate: " + state.name);
                yield return state.State();
            }
        }

        // show leaderboard and exit
    }
}
