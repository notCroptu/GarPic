using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Voting;

public class LeaderBoard : GameState
{
    private NetworkList<NetworkScore> Scores => _scores;
    private NetworkList<NetworkScore> _scores = new NetworkList<NetworkScore>(
                new List<NetworkScore>(),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

    public override IEnumerator State()
    {
        yield return null;
    }
}
