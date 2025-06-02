using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Voting : GameState
{    
    [SerializeField] private Showcase _showcase;
    [SerializeField] private SessionStart _sessionStart;
    [SerializeField] private GameObject _canvas;
    [SerializeField] private float _votingTime = 40f;

    [SerializeField] private TMP_Text _timerTMP;

    [SerializeField] private TMP_Text[] _nickList;
    [SerializeField] private RawImage[] _imageList;
    [SerializeField] private Button[] _buttonList;
    [SerializeField] private GameObject[] _goList;

    private NetworkVariable<int> _timer = new();

    private HashSet<ulong> _doneClients;
    public Dictionary<ulong, List<ulong>> Votes { get; private set; }

    private void Start()
    {
        ResetValues();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("PhotoTaking OnNetworkSpawn IsServer: " + IsServer );

        base.OnNetworkSpawn();

        _timer ??= new();
        _timer.OnValueChanged += (_, _) => UpdateTimer();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        _timer.OnValueChanged -= (_, _) => UpdateTimer();
    }

    public override IEnumerator State()
    {
        yield return base.State();

        _doneClients = new HashSet<ulong>();
        Votes = new();

        Debug.Log("Server started Voting. ");

        SetUpPicturesClientRpc();

        if ( _showcase.RoundPictures.Count > 0
            && _networkSetup.NetworkManager.ConnectedClients.Count > 1)
        {
            float t = _votingTime;
            while ( t > 0f )
            {
                t -= Time.deltaTime;

                int t2 = Mathf.FloorToInt(t);
                if ( t2 != _timer.Value && t2 >= 0)
                    _timer.Value = t2;
            
                yield return null;

                if ( AllClientsDone() )
                    break;
            }
        }

        Debug.Log("Voting turning off. ");

        DisableVotingClientRpc();
    }

    private bool AllClientsDone()
    {
        foreach ( ulong clientID in _networkSetup.NetworkManager.ConnectedClientsIds )
            if ( ! _doneClients.Contains(clientID) )
                return false;
        Debug.Log("All clients are done voting. ");
        return true;
    }

    [ClientRpc]
    public void SetUpPicturesClientRpc()
    {
        foreach(GameObject o in _goList)
            o.SetActive(false);

        Debug.Log("Voting started pic setup from RoundPictures: " + _showcase.RoundPictures.Count);

        int i = -1;

        foreach( KeyValuePair<ulong, Texture2D> pic in _showcase.RoundPictures )
        {
            Debug.Log("Trying to load photo to RoundPictures for " + pic.Key + " texture exists: " + (pic.Value != null) );

            i++;

            if ( pic.Key == _networkSetup.NetworkManager.LocalClientId ) // don't let player vote on themselves
                continue;

            Debug.Log("Voting loaded photo to RoundPictures for " + pic.Key + " texture exists: " + (pic.Value != null) );
            
            _nickList[i].text = _sessionStart.FindNickname(pic.Key);
            _imageList[i].texture = pic.Value;
            _buttonList[i].onClick.AddListener(() =>
                Vote(pic.Key, _networkSetup.NetworkManager.LocalClientId));
            _goList[i].SetActive(true);
        }

        _canvas.SetActive(true);
    }

    [ClientRpc]
    public void DisableVotingClientRpc()
    {
        DisableVoting();
    }

    private void DisableVoting()
    {
        _canvas.SetActive(false);

        foreach(GameObject o in _goList)
            o.SetActive(false);

        foreach(Button b in _buttonList)
            b.onClick.RemoveAllListeners();
    }

    [ServerRpc(RequireOwnership = false)]
    private void VoteServerRpc(ulong votedId, ServerRpcParams rpcParams = default)
    {
        Vote(votedId, rpcParams.Receive.SenderClientId);
    }

    public void Vote(ulong votedId, ulong voterId)
    {
        if ( IsServer || IsHost )
        {
            Debug.Log(" Voting on " + votedId + " from: " + voterId);

            if (!Votes.ContainsKey(votedId))
                Votes[votedId] = new List<ulong>();
            
            Votes[votedId].Add(voterId);
            _doneClients.Add(voterId);
        }
        else
            VoteServerRpc(votedId);

        if ( (!IsServer && !IsHost) || voterId == NetworkManager.ServerClientId )
            DisableVoting();
    }

    private void UpdateTimer()
    {
        if ( ! _timerTMP.gameObject.activeSelf )
            _timerTMP.gameObject.SetActive(true);

        _timerTMP.text = _timer.Value.ToString();
    }

    public override void ResetValues()
    {
        base.ResetValues();

        _canvas.SetActive(false);
        
        foreach(GameObject o in _goList)
            o.SetActive(false);
        
        Debug.Log("Start up voting. Networks: " + _timer.Value);
    }
}
