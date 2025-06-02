using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitSession : MonoBehaviour
{
    [SerializeField] private GameObject _exitSession;
    private NetworkSetup _networkSetup;
    private void Start()
    {
        _networkSetup = FindFirstObjectByType<NetworkSetup>();
    }

    private void Update()
    {
        if ( !_exitSession.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            _exitSession.SetActive(true);
    }

    public void ExitToLobby()
    {
        if ( _networkSetup.NetworkManager.IsHost || _networkSetup.NetworkManager.IsServer)
        {
            _networkSetup.NetworkManager.Shutdown(true); // destroy player objects
            SceneManager.LoadScene("Lobby");
        }
        else if ( _networkSetup.NetworkManager.IsClient)
            _networkSetup.NetworkManager.Shutdown();
        
        // It should call the on disconnect method in network setup
    }
}
