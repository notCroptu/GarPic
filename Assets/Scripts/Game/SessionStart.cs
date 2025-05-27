using System;
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
    [SerializeField] private GameObject _playerList;
    [SerializeField] private TMP_InputField _InputField;

    private NetworkList<NetworkNickname> _nicknames = new();
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

    private void Start()
    {
        _canvas.SetActive(true);

        _networkSetup = FindFirstObjectByType<NetworkSetup>();
        _sessionCode.text = _networkSetup.SessionCode;

        if ( ! NetworkManager.Singleton.IsHost )
        {
            _startGame.gameObject.SetActive(false);
        }
        else
        {
            _startGame.interactable = false;
            _startGame.onClick.AddListener(OnStart);
            _startGame.onClick.AddListener(_gameLoop.StartGame);

            NetworkManager.Singleton.OnClientConnectedCallback += AddPlayer;
            NetworkManager.Singleton.OnClientDisconnectCallback += RemovePlayer;
        }

        _nicknames ??= new NetworkList<NetworkNickname>();

        _nicknames.OnListChanged += UpdatePlayerList;

        _InputField.text = NetworkManager.Singleton.LocalClientId.ToString();
    }

    public void UpdatePlayerList(NetworkListEvent<NetworkNickname> change)
    {
        // ui stuff
        // update players and add edit button to player bar
    }

    /// <summary>
    /// on pressing the confirm button to change nick tells server to update network list
    /// </summary>
    public void UpdatePlayerNickname()
    {
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
                var entry = _nicknames[i];
                entry.nickname = nickname;
                _nicknames[i] = entry;
                break;
            }
        }
    }

    /// <summary>
    /// Only host should be accessing these
    /// </summary>
    public void AddPlayer(ulong playerID)
    {
        NetworkNickname nickname = new NetworkNickname();
        nickname.clientId = playerID;
        _nicknames.Add(nickname);

        UpdateStartButton(_nicknames.Count);
    }

    public void RemovePlayer(ulong playerID)
    {
        UpdateStartButton(_nicknames.Count);
    }

    /// <summary>
    /// Only host can start game
    /// </summary>
    public void UpdateStartButton( int currentPlayers )
    {
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
        if ( NetworkManager.Singleton.IsHost )
        {
            NetworkManager.Singleton.OnClientConnectedCallback += AddPlayer;
            NetworkManager.Singleton.OnClientDisconnectCallback += RemovePlayer;
        }
    }
}
