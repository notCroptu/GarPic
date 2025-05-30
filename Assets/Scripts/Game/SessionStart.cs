using System;
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

    private NetworkList<NetworkNickname> _nicknames;
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

        _nicknames ??= new NetworkList<NetworkNickname>(
                new List<NetworkNickname>(),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        if ( IsHost || IsServer )
        {
            Debug.Log("Started as host. ");

            _startGame.interactable = false;
            _startGame.onClick.AddListener(OnStart);
            _startGame.onClick.AddListener(_gameLoop.StartGame);

            NetworkManager.Singleton.OnClientConnectedCallback += AddPlayer;
            NetworkManager.Singleton.OnClientDisconnectCallback += RemovePlayer;

            AddPlayer(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            _startGame.gameObject.SetActive(false);
        }

        _InputField.text = NetworkManager.Singleton.LocalClientId.ToString();

        _nicknames.OnListChanged += UpdatePlayerList;
        UpdatePlayerList(default);
    }

    private void Start()
    {
        _canvas.SetActive(true);

        _networkSetup = FindFirstObjectByType<NetworkSetup>();
        _sessionCode.text = _networkSetup.SessionCode;
    }

    /// <summary>
    /// on pressing the confirm button to change nick tells server to update network list
    /// </summary>
    public void UpdatePlayerNickname()
    {
        Debug.Log("host? " + IsHost + " Starting set nick from: " + NetworkManager.Singleton.LocalClientId + " new nick: " + _InputField.text);
        SetNicknameServerRpc(_InputField.text);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNicknameServerRpc(string nickname, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < _nicknames.Count; i++)
        {
            if (_nicknames[i].clientId == clientId)
            {
                Debug.Log("host? Setting player nick: " + nickname);
                NetworkNickname entry = _nicknames[i];
                entry.nickname = nickname;
                _nicknames[i] = entry;
                break;
            }
        }
    }

    /// <summary>
    /// Update players and add edit button to player bar.
    /// </summary>
    public void UpdatePlayerList(NetworkListEvent<NetworkNickname> change)
    {
        Debug.Log("Nicknames OnDeltaListChanged: " + change.Type);

        foreach ( TMP_Text text in _playerList )
            text.transform.parent.gameObject.SetActive(false);
        
        for ( int i = 0 ; i < _nicknames.Count ; i++ )
        {
            if ( _playerList.Length <= i ) break;

            _playerList[i].transform.parent.gameObject.SetActive(true);
            _playerList[i].text = _nicknames[i].nickname.ToString();

            if ( _nicknames[i].clientId == NetworkManager.Singleton.LocalClientId )
            {
                Vector3 pos = _editNickname.transform.position;
                pos.y = _playerList[i].transform.position.y;
                _editNickname.transform.position = pos;
            }
        }
    }

    // the 3 next methods ar only available for host

    /// <summary>
    /// Only host should be accessing these
    /// </summary>
    public void AddPlayer(ulong playerID)
    {
        if ( ! IsServer ) return; // the init on OnNetworkSpawn should block the usage of add and remove from anyone other than host but prevent.

        NetworkNickname nickname = new() { clientId = playerID };
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

        _startGame.interactable = currentPlayers >= 2; // 2 phones for now, should be 3
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
        _canvas.SetActive(false);
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && IsHost )
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AddPlayer;
            NetworkManager.Singleton.OnClientDisconnectCallback -= RemovePlayer;
        }
    }
}
