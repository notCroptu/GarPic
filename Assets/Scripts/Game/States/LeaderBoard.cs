using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LeaderBoard : GameState
{
    [SerializeField] private GameObject _canvas;
    [SerializeField] private float _leaderBoardTime = 6f;
    [SerializeField] private GameLoop _gameloop;
    [SerializeField] private SessionStart _sessionStart;
    [SerializeField] private Voting _voting;
    [SerializeField] private TMP_Text[] _nickList;
    [SerializeField] private TMP_Text[] _scoreList;
    [SerializeField] private GameObject[] _goList;
    [SerializeField] private GameObject[] _layoutList;

    private NetworkList<NetworkScore> _scores = new NetworkList<NetworkScore>(
                new List<NetworkScore>(),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

    public struct NetworkScore : INetworkSerializable, IEquatable<NetworkScore>
    {
        public ulong clientId;
        public int score;

        public bool Equals(NetworkScore other)
        {
            return clientId == other.clientId && score.Equals(other.score);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref score);
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

        _scores.OnListChanged += UpdateLeaderBoard;

        if ( IsHost || IsServer )
        {
            Debug.Log("Started as host. ");

            _networkSetup.NetworkManager.OnClientConnectedCallback += AddPlayer;
            _networkSetup.NetworkManager.OnClientDisconnectCallback += RemovePlayer;

            AddPlayer(_networkSetup.NetworkManager.LocalClientId);
        }
    }

    public void AddPlayer(ulong playerID)
    {
        if ( ! IsServer ) return;

        NetworkScore score = new() { clientId = playerID, score = 0 };
        _scores.Add(score);
    }

    public void RemovePlayer(ulong playerID)
    {
        if ( ! IsServer ) return;

        for (int i = 0; i < _scores.Count; i++)
        {
            if (_scores[i].clientId == playerID)
            {
                _scores.Remove(_scores[i]);
                Debug.Log("host? Removed player: " + playerID + " nick count now: " + _scores.Count);
                break;
            }
        }
    }

    public override IEnumerator State()
    {
        Debug.Log("Trying to start Leaderboard from host. ");

        yield return base.State();

        Debug.Log("Start Leaderboard from host. ");

        ShowLeaderBoardClientRpc();

        if ( _voting.Votes.Count != 0 )
        {
            ulong winnerId = _voting.Votes.FirstOrDefault().Key;
            List<ulong> voters = _voting.Votes.FirstOrDefault().Value;

            // find the client with most votes
            foreach (KeyValuePair<ulong, List<ulong>> vote in _voting.Votes)
            {
                if ( voters.Count < vote.Value.Count )
                {
                    winnerId = vote.Key;
                    voters = vote.Value;
                }
            }

            Dictionary<ulong, int> newScores = new Dictionary<ulong, int>();
            newScores[winnerId] = _voting.Votes[winnerId].Count;

            // each voter gets one point
            foreach (ulong voterId in voters)
                newScores[voterId] = 1;

            // update network list with new votes
            for ( int i = 0 ; i < _scores.Count ; i++ )
            {
                if ( ! newScores.ContainsKey(_scores[i].clientId) ) continue;
                
                _scores[i] = new NetworkScore {
                    clientId = _scores[i].clientId,
                    score = _scores[i].score + newScores[ _scores[i].clientId ]
                    };
            }
        }

        Debug.Log("Finished, starting countdown. ");
        yield return new WaitForSeconds(_leaderBoardTime);

        if ( !_gameloop.FinalRound )
            DisableLeaderBoardClientRpc();
    }

    [ClientRpc]
    public void ShowLeaderBoardClientRpc()
    {
        Debug.Log("Client showing Leaderboard ");
        _canvas.SetActive(true);
    }

    [ClientRpc]
    public void DisableLeaderBoardClientRpc()
    {
        Debug.Log("Client disabling Leaderboard ");
        _canvas.SetActive(false);
    }

    private void UpdateLeaderBoard(NetworkListEvent<NetworkScore> change)
    {
        Debug.Log("Updated score list with action: " + change.Type);
        
        // Get scores in descending order
        List<NetworkScore> scoresList = new List<NetworkScore>(_scores.Count);
        for (int i = 0; i < _scores.Count; i++)
            scoresList.Add(_scores[i]);
        List<NetworkScore> orderedScores = scoresList.OrderByDescending(s => s.score).ToList();

        // foreach (GameObject go in _goList)
            // go.SetActive(false);

        // For some reason deactivating and then activating again is keeping all gos inactive
        HashSet<GameObject> gos = _goList.ToHashSet();

        // Animate each row to its new position
        for (int rank = 0; rank < orderedScores.Count; rank++)
        {
            NetworkScore score = orderedScores[rank];
            int goIndex = FindPlayerIndex(score.clientId);

            if (goIndex < 0 || goIndex >= _goList.Length) continue;

            _goList[goIndex].SetActive(true);
            gos.Remove(_goList[goIndex]);
            _nickList[goIndex].text = _sessionStart.FindNickname(score.clientId);
            _scoreList[goIndex].text = score.score.ToString();

            RectTransform moving = _goList[goIndex].GetComponent<RectTransform>();
            RectTransform target = _layoutList[rank].GetComponent<RectTransform>();
            StartCoroutine(AnimateMoveToTarget(moving, target.anchoredPosition));
        }

        foreach ( GameObject go in gos)
            go.SetActive(false);
    }

    private int FindPlayerIndex(ulong clientId)
    {
        // Maps a clientId to the UI array index, even if it's not correct when it shows the leaderboard again,
        // It gives convincing visual feedback that the game is progressing
        foreach ( ulong id in _networkSetup.NetworkManager.ConnectedClientsIds )
        {
            if ( id == clientId)
                return (int) id;
        }
        return -1;
    }

    private IEnumerator AnimateMoveToTarget(RectTransform tr, Vector2 targetPos)
    {
        Vector2 startPos = tr.anchoredPosition;
        float t = 0f, duration = 0.4f;
        while (t < duration)
        {
            t += Time.deltaTime;
            tr.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        tr.anchoredPosition = targetPos;
    }

    public override void ResetValues()
    {
        base.ResetValues();

        if ( _scores != null )
        {
            for ( int i = 0 ; i < _scores.Count ; i++ )
                _scores[i] = new NetworkScore { clientId = _scores[i].clientId, score = 0 };
        }
        
        _canvas.SetActive(false);
        Debug.Log("Start up session start. Networks: " + _scores.Count);
    }
}
