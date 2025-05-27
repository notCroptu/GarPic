using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionStart : MonoBehaviour
{
    [SerializeField] private TMP_Text _sessionCode;
    [SerializeField] private Button _startGame;
    [SerializeField] private GameObject _canvas;
    [SerializeField] private NetworkSetup _networkSetup;
    [SerializeField] private GameLoop _gameLoop;
    [SerializeField] private GameObject _playerList;

    private void Start()
    {
        _canvas.SetActive(true);

        if ( _networkSetup == null )
            _networkSetup = FindFirstObjectByType<NetworkSetup>();
        
        _sessionCode.text = _networkSetup.SessionCode;
        _startGame.interactable = false;
        _startGame.onClick.AddListener(OnStart);
        _startGame.onClick.AddListener(_gameLoop.StartGame);
    }

    public void UpdateStartButton( int currentPlayers )
    {
        _startGame.interactable = currentPlayers > 3;
    }

    public void OnStart()
    {
        _canvas.SetActive(false);
    }

    private void OnDestroy()
    {
        _networkSetup.OnPlayerChange -= UpdateStartButton;
    }
}
