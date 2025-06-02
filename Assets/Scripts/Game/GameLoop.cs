using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameLoop : NetworkBehaviour
{
    [SerializeField] private GameState[] _states;
    [SerializeField] private Button _startOver;
    [SerializeField] private TMP_Text _waiting;
    [SerializeField] private int _gameRounds = 8;
    public bool FinalRound { get; private set; } = false;
    public NetworkVariable<int> Round { get; private set; } = new();

    private void Start()
    {
        // StartCoroutine(GameLoopCoroutine());
        _startOver.gameObject.SetActive(false);
        _waiting.gameObject.SetActive(false);
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
            FinalRound = _gameRounds == i;
            Round.Value = i;
            foreach (GameState state in _states)
            {
                Debug.Log("Starting gamestate: " + state);
                yield return state.State();
            }
        }

        _startOver.gameObject.SetActive(true);
        SetUpPicturesClientRpc();
    }

    [ClientRpc]
    public void SetUpPicturesClientRpc()
    {
        if ( IsHost || IsServer ) return;

        _waiting.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        foreach (GameState state in _states)
            state.ResetValues();
        
        FindFirstObjectByType<NetworkSetup>().StartGame();
    }
}
