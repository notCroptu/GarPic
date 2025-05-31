using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TimerDisplay : NetworkBehaviour
{
    [SerializeField] private TMP_Text[] _timerList;
    [SerializeField] private SessionStart _sessionStart;

    private List<GPSTimer> _playerTimers;
    private Dictionary<ulong, int> _clientToIndex;

    private NetworkManager _networkManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _playerTimers = new();
        _clientToIndex = new();

        _networkManager = FindFirstObjectByType<NetworkManager>();

        _networkManager.OnClientConnectedCallback += AddPlayer;
        _networkManager.OnClientDisconnectCallback += RemovePlayer;

        StartCoroutine(AfterSpawnCoroutine());
    }
    
    private IEnumerator AfterSpawnCoroutine()
    {
        yield return null;
        FindAllPlayerTimers();
    }

    private void Update()
    {
        foreach ( TMP_Text timer in _timerList )
            timer.text = "";

        foreach ( GPSTimer playerTimer in _playerTimers )
        {
            ulong clientId = playerTimer.OwnerClientId;

            if ( _clientToIndex.TryGetValue(clientId, out int i ))
                _timerList[i].text = playerTimer.Timer.Value.ToString("F1");
        }
    }

    /// <summary>
    /// looks for GPSTimer components from remote players, needs to be in networkobject to work
    /// matches them with known nicknames, and maps them to UI elements. Called when new players or remove player
    /// </summary>
    private void FindAllPlayerTimers()
    {
        _playerTimers.Clear();
        _clientToIndex.Clear();

        GPSTimer[] found = FindObjectsByType<GPSTimer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Debug.Log("Found " + found.Length + " GPSTimers");

        for ( int i = 0; i < found.Length; i++ )
        {
            GPSTimer timer = found[i];

            if ( timer.OwnerClientId == _networkManager.LocalClientId ) continue; // don't track self

            _playerTimers.Add(timer);

            string nickname = _sessionStart.FindNickname(timer.OwnerClientId);

            for ( int j = 0; j < _sessionStart.PlayerList.Length; j++ )
            {
                if ( _sessionStart.PlayerList[j].text == nickname )
                {
                    _clientToIndex[timer.OwnerClientId] = j;
                    break;
                }
            }
        }
    }

    private void AddPlayer(ulong _)
    {
        FindAllPlayerTimers();
    }

    private void RemovePlayer(ulong clientId)
    {
        _playerTimers.RemoveAll(timer => timer.OwnerClientId == clientId);
        _clientToIndex.Remove(clientId);
    }
}
