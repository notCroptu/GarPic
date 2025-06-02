using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class GameState : NetworkBehaviour
{
    protected NetworkSetup _networkSetup;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _networkSetup = FindFirstObjectByType<NetworkSetup>();
    }

    public virtual IEnumerator State()
    {
        Debug.Log("starting State. ");

        if ( ! IsServer && ! IsHost ) yield break;

        Debug.Log("starting State. and is server or host. ");
    }

    public virtual void ResetValues()
    {

    }
}