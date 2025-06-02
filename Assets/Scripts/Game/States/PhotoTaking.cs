using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PhotoTaking : GameState
{
    [SerializeField] private GameLoop _gameloop;
    [SerializeField] private RawImage _display;
    [SerializeField] private Button _takePhoto;
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameObject _gpsTimerPrefab;
    private GPSTimer _timer;
    private WebCamTexture _webcamTexture;
    [SerializeField] private Texture2D _nullTexture;

    private byte[] _imageBytes;
    private Texture2D _photo;
    private HashSet<ulong> _doneClients;

    public override void OnNetworkSpawn()
    {
        Debug.Log("PhotoTaking OnNetworkSpawn IsServer: " + IsServer );

        base.OnNetworkSpawn();

        if ( IsServer || IsHost )
        {
            AddPlayer(_networkSetup.NetworkManager.LocalClientId);
            _networkSetup.NetworkManager.OnClientConnectedCallback += AddPlayer;

            StartCoroutine(DeleteFolder(_networkSetup.SessionCode));
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsHost || IsServer)
        {
            if (_networkSetup.NetworkManager != null)
            {
                _networkSetup.NetworkManager.OnClientConnectedCallback -= AddPlayer;
            }
        }
    }

    public void AddPlayer(ulong clientId)
    {
        Debug.Log("Adding GPSTimer from server.");

        GameObject TimerObj = Instantiate(_gpsTimerPrefab);

        TimerObj.GetComponent<NetworkObject>()
            .SpawnWithOwnership(clientId);
    }

    public void SetGPS(GPSTimer timer)
    {
        _timer = timer;
    }

    private IEnumerator DelayedCameraInitialization()
    {
        yield return null;

        #if !UNITY_EDITOR
        yield return AskCameraPermission();
        InitializeCamera();
        #endif
    }

    private IEnumerator AskCameraPermission()
    {
        #if UNITY_EDITOR
        yield return new WaitUntil( () => UnityEditor.EditorApplication.isRemoteConnected );

        #elif UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
        }

        yield return new WaitUntil( () =>
            UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)); 
        #endif
    }

    private void InitializeCamera()
    {
        Debug.Log("Photo Taking initializing Camera. ");
        _webcamTexture = new WebCamTexture();
        _display.color = Color.white;
        _display.texture = _webcamTexture;

        if ( _webcamTexture.isPlaying )
            _webcamTexture.Stop();
        
        _webcamTexture.Play();
    }


    private void Start()
    {
        StartCoroutine(AskCameraPermission());

        _takePhoto.onClick.AddListener(TakeSnapshot);
        _canvas.SetActive(false);
    }

    public void TakeSnapshot()
    {
        #if UNITY_EDITOR
        if ( _imageBytes == null )
        {
            _imageBytes = _nullTexture.EncodeToPNG();
        }
        #elif UNITY_ANDROID
        if ( _webcamTexture != null && _webcamTexture.isPlaying )
        {
            _photo = new Texture2D(_webcamTexture.width, _webcamTexture.height);
            _photo.SetPixels32(_webcamTexture.GetPixels32());
            _photo.Apply();
            _imageBytes = _photo.EncodeToPNG();
        }
        #endif

        Debug.Log("PhotoTaking image bytes set: " + _imageBytes != null);

        _canvas.SetActive(false);
        _timer.StopCoroutine();
    }


    public override IEnumerator State()
    {
        yield return base.State();

        _doneClients = new HashSet<ulong>();

        Debug.Log("Server started PhotoTaking. ");

        ClientStateClientRpc();
        yield return new WaitUntil( () => AllClientsDone() );
    }

    [ClientRpc]
    public void ClientStateClientRpc()
    {
        StartCoroutine(ClientStateCoroutine());
    }

    private bool AllClientsDone()
    {
        foreach ( ulong clientID in _networkSetup.NetworkManager.ConnectedClientsIds )
            if ( ! _doneClients.Contains(clientID) )
                return false;
        Debug.Log("All clients are done taking a photo. ");
        return true;
    }

    public IEnumerator ClientStateCoroutine()
    {
        Debug.Log("Client started PhotoTaking. ");

        _canvas.SetActive(true);

        yield return DelayedCameraInitialization();

        // wait until count is done or picture is taken
        yield return _timer.StartCount();

        _canvas.SetActive(false);

        if ( _webcamTexture != null && _webcamTexture.isPlaying )
            _webcamTexture.Stop();
        
        yield return null;
        _display.texture = _photo;

        yield return UploadPhoto();

        if ( IsHost || IsServer )
            _doneClients.Add(_networkSetup.NetworkManager.LocalClientId);
        else
            ClientDoneServerRpc();

        _imageBytes = null;
        _photo = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClientDoneServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("player: " + rpcParams.Receive.SenderClientId + " done taking photo. Done Count: " + _doneClients.Count);
        _doneClients.Add(rpcParams.Receive.SenderClientId);
    }

    public IEnumerator UploadPhoto()
    {
        if ( LoadSupabaseConfig.Config == null )
        {
            Debug.Log("PhotoTaking supabase config file not found. ");
            yield break;
        }

        if (_imageBytes == null)
        {
            Debug.Log("PhotoTaking no photo to upload!");
            yield break;
        }

        string fileName = $"{_networkSetup.SessionCode}/{_networkSetup.NetworkManager.LocalClientId}/photo_{_gameloop.Round.Value}.png";
        string url = $"{LoadSupabaseConfig.Config.supabaseURL}/storage/v1/object/{LoadSupabaseConfig.Config.supabaseBUCKET}/{fileName}";

        Debug.Log("PhotoTaking url: "  + url);

        int maxTries = 3;
        int attempt = 0;
        bool success = false;

        while ( attempt < maxTries && !success )
        {
            using (UnityWebRequest www = UnityWebRequest.Put(url, _imageBytes))
            {
                www.SetRequestHeader("apikey", LoadSupabaseConfig.Config.supabaseANON);
                www.SetRequestHeader("Authorization", $"Bearer {LoadSupabaseConfig.Config.supabaseANON}");
                www.SetRequestHeader("Content-Type", "image/png");

                Debug.Log($"PhotoTaking attempt {attempt + 1}");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    success = true;
                    Debug.Log("done PhotoTaking");
                }
                else
                {
                    Debug.LogWarning("Photo upload failed: " + www.error);
                }
            }
            attempt++;
        }

        Debug.Log("done PhotoTaking");
    }

    public override void ResetValues()
    {
        base.ResetValues();
        StartCoroutine(DeleteFolder(_networkSetup.SessionCode));
    }
    
    IEnumerator DeleteFolder(string folderName)
    {
        string path = $"{LoadSupabaseConfig.Config.supabaseURL}/storage/v1/object/{LoadSupabaseConfig.Config.supabaseBUCKET}/";

        Debug.Log("Photo taking delete url: "  + path);

        for (int i = 0; i < 8; i++)
        {
            for (int j = 1; j <= 8; j++)
            {
                string url = $"{path}{folderName}/{i}/photo_{j}.png";
                Debug.Log("Photo taking delete url: "  + url);

                UnityWebRequest www = UnityWebRequest.Delete(url);
                www.SetRequestHeader("apikey", LoadSupabaseConfig.Config.supabaseANON);
                www.SetRequestHeader("Authorization", $"Bearer {LoadSupabaseConfig.Config.supabaseANON}");
                www.SetRequestHeader("Content-Type", "image/png");

                yield return www.SendWebRequest();

                if ( www.result != UnityWebRequest.Result.Success )
                {
                    Debug.Log("Error deleting " + i + " " + j + " file: " + www.error);
                }
            }
        }
    }
}