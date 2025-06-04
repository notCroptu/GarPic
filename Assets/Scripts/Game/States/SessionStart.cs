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
    [field:SerializeField] public TMP_Text[] PlayerList { get; private set; }
    [SerializeField] private Button _editNickname;
    [SerializeField] private TMP_InputField _InputField;

    private NetworkList<NetworkNickname> _nicknames = new NetworkList<NetworkNickname>(
                new List<NetworkNickname>(),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
    private NetworkSetup _networkSetup;

    public string ClientNick { get; private set; }

    public Action UpdateList;

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
        ResetValues();
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
        ClientNick = "P" + _networkSetup.NetworkManager.LocalClientId.ToString();
        _InputField.text = ClientNick;

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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (_nicknames != null)
            _nicknames.OnListChanged -= UpdatePlayerList;

        if (IsHost || IsServer)
        {
            if (_networkSetup.NetworkManager != null)
            {
                _networkSetup.NetworkManager.OnClientConnectedCallback -= AddPlayer;
                _networkSetup.NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
            }
        }
    }

    /// <summary>
    /// UI isn't finished setting real positions until later, so update then
    /// </summary>
    private IEnumerator DelayUI()
    {
        yield return null;
        UpdatePlayerList(default);
    }

    /// <summary>
    /// on pressing the confirm button to change nick tells server to update network list
    /// </summary>
    public void UpdatePlayerNickname()
    {
        ulong clientId = _networkSetup.NetworkManager.LocalClientId;
        string newNickname = _InputField.text;

        // Prevent spamming the done button
        if ( FindNickname(clientId) == newNickname )
            return;

        Debug.Log("host? " + (IsServer || IsHost) + " Starting set nick from: " + _networkSetup.NetworkManager.LocalClientId + " new nick: " + _InputField.text);

        if (IsServer)
            SetNickname(clientId, newNickname);
        else
            SetNicknameServerRpc(newNickname);

        ClientNick = newNickname;
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
        
        for ( int i = 0 ; i < PlayerList.Length ; i++ )
        {
            if ( i < _nicknames.Count )
            {
                PlayerList[i].transform.parent.gameObject.SetActive(true);
                PlayerList[i].text = _nicknames[i].nickname.ToString();

                if ( _nicknames[i].clientId == _networkSetup.NetworkManager.LocalClientId )
                {
                    Debug.Log("Setting edit button postion");
                    Vector3 pos = _editNickname.transform.position;
                    pos.y = PlayerList[i].transform.position.y;
                    _editNickname.transform.position = pos;
                }
            }
            else if ( PlayerList[i].transform.parent.gameObject.activeSelf )
                PlayerList[i].transform.parent.gameObject.SetActive(false);
        }

        UpdateList?.Invoke();
    }

    public event Action<string> LoginPlayer;
    public event Action<string> LogoutPlayer;

    // the 3 next methods ar only available for host

    /// <summary>
    /// Only host should be accessing these
    /// </summary>
    public void AddPlayer(ulong playerID)
    {
        if ( ! IsServer ) return; // the init on OnNetworkSpawn should block the usage of add and remove from anyone other than host but prevent.

        NetworkNickname nickname = new() { clientId = playerID, nickname = "P" + playerID.ToString() };
        _nicknames.Add(nickname);

        LoginPlayer?.Invoke(nickname.nickname.ToString());

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
                LogoutPlayer?.Invoke(_nicknames[i].nickname.ToString());

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

        _startGame.interactable = currentPlayers >= 2; // 1 phones for now, should be 3, will leave 2 for demonstration purposes
    }

    public string FindNickname(ulong clientId)
    {
        foreach ( NetworkNickname client in _nicknames )
            if ( client.clientId == clientId )
                return client.nickname.ToString();
        
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
        _sessionCode.gameObject.SetActive(false);
        _startGame.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_networkSetup.NetworkManager != null && IsHost )
        {
            _networkSetup.NetworkManager.OnClientConnectedCallback -= AddPlayer;
            _networkSetup.NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
        }
    }

    public void ResetValues()
    {
        _sessionCode.gameObject.SetActive(true);

        if ( IsHost || IsServer )
            _startGame.gameObject.SetActive(true);
        else
            _startGame.gameObject.SetActive(false);
        
        _canvas.SetActive(true);
        Debug.Log("Start up session start. Networks: " + _nicknames.Count);
    }
}
