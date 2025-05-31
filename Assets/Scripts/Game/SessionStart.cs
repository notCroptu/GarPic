using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SessionStart : NetworkBehaviour
{
    [SerializeField] private TMP_Text _sessionCode;
    [SerializeField] private Button _startGame;
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameLoop _gameLoop;
    [SerializeField] private TMP_Text[] _playerList;
    [SerializeField] private Button _editNickname;
    [SerializeField] private TMP_InputField _InputField;

    private NetworkList<NetworkNickname> _nicknames = new NetworkList<NetworkNickname>(
                new List<NetworkNickname>(),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
    private NetworkSetup _networkSetup;

    public struct NetworkNickname : INetworkSerializable, IEquatable<NetworkNickname>
    {
        public ulong clientId;
        public FixedString32Bytes nickname;

        public bool Equals(NetworkNickname other)
        {
            return clientId == other.clientId && nickname.Equals(other.nickname);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref nickname);
        }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn IsServer: " + IsServer );

        base.OnNetworkSpawn();

        _networkSetup = FindFirstObjectByType<NetworkSetup>();
        _sessionCode.text = _networkSetup.SessionCode;

        /* _nicknames ??= new NetworkList<NetworkNickname>(
                new List<NetworkNickname>(),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );*/
        _nicknames.OnListChanged += UpdatePlayerList;
        _InputField.text = _networkSetup.NetworkManager.LocalClientId.ToString();

        if ( IsHost || IsServer )
        {
            Debug.Log("Started as host. ");

            _startGame.interactable = false;
            _startGame.onClick.AddListener(OnStart);

            _networkSetup.NetworkManager.OnClientConnectedCallback += AddPlayer;
            _networkSetup.NetworkManager.OnClientDisconnectCallback += RemovePlayer;

            AddPlayer(_networkSetup.NetworkManager.LocalClientId);
        }
        else
        {
            _startGame.gameObject.SetActive(false);
        }

        StartCoroutine( DelayUI() );
    }

    /// <summary>
    /// UI isn't finished setting real positions until later, so update then
    /// </summary>
    private IEnumerator DelayUI()
    {
        yield return null;
        UpdatePlayerList(default);
    }

    private void Start()
    {
        _canvas.SetActive(true);
    }

    /// <summary>
    /// on pressing the confirm button to change nick tells server to update network list
    /// </summary>
    public void UpdatePlayerNickname()
    {
        ulong clientId = _networkSetup.NetworkManager.LocalClientId;
        string newNickname = _InputField.text;

        // Prevent spamming the done button
        if (FindNickname(clientId).nickname == newNickname)
            return;

        Debug.Log("host? " + (IsServer || IsHost) + " Starting set nick from: " + _networkSetup.NetworkManager.LocalClientId + " new nick: " + _InputField.text);

        if (IsServer)
            SetNickname(clientId, newNickname);
        else
            SetNicknameServerRpc(newNickname);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetNicknameServerRpc(string nickname, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log("ServerRpc Set nickname: " + clientId + " new nick: " + nickname);
        SetNickname(clientId, nickname);
    }

    public void SetNickname(ulong clientId, string nickname)
    {
        Debug.Log("host? " + (IsServer || IsHost) + " Setting set nick from: " + clientId + " new nick: " + nickname);

        for (int i = 0; i < _nicknames.Count; i++)
        {
            if (_nicknames[i].clientId == clientId)
            {
                Debug.Log("host? Setting player nick: " + nickname);
                _nicknames[i] = new NetworkNickname { clientId = clientId, nickname = nickname };
                return;
            }
        }

        // if not found
        NetworkNickname nick = new() { clientId = clientId, nickname = nickname };
        _nicknames.Add(nick);
    }

    /// <summary>
    /// Update players and add edit button to player bar.
    /// </summary>
    public void UpdatePlayerList(NetworkListEvent<NetworkNickname> change)
    {
        Debug.Log("Nicknames OnDeltaListChanged: " + change.Type);
        
        for ( int i = 0 ; i < _playerList.Length ; i++ )
        {
            if ( i < _nicknames.Count )
            {
                _playerList[i].transform.parent.gameObject.SetActive(true);
                _playerList[i].text = _nicknames[i].nickname.ToString();

                if ( _nicknames[i].clientId == _networkSetup.NetworkManager.LocalClientId )
                {
                    Debug.Log("Setting edit button postion");
                    Vector3 pos = _editNickname.transform.position;
                    pos.y = _playerList[i].transform.position.y;
                    _editNickname.transform.position = pos;
                }
            }
            else if ( _playerList[i].transform.parent.gameObject.activeSelf )
                _playerList[i].transform.parent.gameObject.SetActive(false);
        }
    }

    // the 3 next methods ar only available for host

    /// <summary>
    /// Only host should be accessing these
    /// </summary>
    public void AddPlayer(ulong playerID)
    {
        if ( ! IsServer ) return; // the init on OnNetworkSpawn should block the usage of add and remove from anyone other than host but prevent.

        NetworkNickname nickname = new() { clientId = playerID, nickname = "P" + playerID.ToString() };
        _nicknames.Add(nickname);

        Debug.Log("host? Added new player: " + playerID + " nick count now: " + _nicknames.Count);

        UpdateStartButton(_nicknames.Count);
    }

    public void RemovePlayer(ulong playerID)
    {
        if ( ! IsServer ) return;

        for (int i = 0; i < _nicknames.Count; i++)
        {
            if (_nicknames[i].clientId == playerID)
            {
                _nicknames.Remove(_nicknames[i]);
                Debug.Log("host? Removed player: " + playerID + " nick count now: " + _nicknames.Count);
                break;
            }
        }
        UpdateStartButton(_nicknames.Count);
    }

    /// <summary>
    /// Only host can start game
    /// </summary>
    public void UpdateStartButton( int currentPlayers )
    {
        if ( ! IsServer ) return;

        _startGame.interactable = currentPlayers >= 1; // 1 phones for now, should be 3
    }

    public NetworkNickname FindNickname(ulong clientId)
    {
        foreach ( NetworkNickname client in _nicknames )
            if ( client.clientId == clientId )
                return client;
        
        return default;
    }

    public void OnStart()
    {
        DisableSessionLobbyClientRpc();
        _gameLoop.StartGame();
    }

    [ClientRpc]
    public void DisableSessionLobbyClientRpc()
    {
        _canvas.SetActive(false);
    }

    private void OnDisable()
    {
        if (_networkSetup.NetworkManager != null && IsHost )
        {
            _networkSetup.NetworkManager.OnClientConnectedCallback -= AddPlayer;
            _networkSetup.NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
        }
    }
}
