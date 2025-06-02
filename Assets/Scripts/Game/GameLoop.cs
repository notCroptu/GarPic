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
    [SerializeField] private SessionStart _sessionStart;
    [SerializeField] private int _gameRounds = 8;
    public bool FinalRound { get; private set; } = false;
    public NetworkVariable<int> Round { get; private set; } = new();

    private void Start()
    {
        // StartCoroutine(GameLoopCoroutine());
        _startOver.gameObject.SetActive(false);
        _waiting.gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn IsServer: " + IsServer );

        base.OnNetworkSpawn();

        if ( IsHost || IsServer )
            _startOver.onClick.AddListener( RestartGame );
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

        RestartClientRpc();
        
        _startOver.gameObject.SetActive(true);
    }

    [ClientRpc]
    public void RestartClientRpc()
    {
        if ( IsHost || IsServer ) return;

        _waiting.gameObject.SetActive(true);
    }

    public void RestartGame()
    {   
        StartClientRpc();
    }

    [ClientRpc]
    public void StartClientRpc()
    {
        foreach (GameState state in _states)
            state.ResetValues();

        _sessionStart.ResetValues();

        Start();
    }
}
