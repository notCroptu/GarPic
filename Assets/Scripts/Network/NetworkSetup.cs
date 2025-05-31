using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Diagnostics;
using UnityEditor;
using System.IO;
using System.Linq;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
#endif

#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif

public class NetworkSetup : MonoBehaviour
{
    [SerializeField] private UnityTransport _transport;
    [field:SerializeField] public NetworkManager NetworkManager { get; private set; }
    [SerializeField] private int _maxPlayers;
    [SerializeField] private string _gameScene = "GameScene";
    public bool IsRelay { get; private set; } = false;
    public bool IsServer  { get; private set; } = false;
    public string SessionCode { get; private set; }

    public Action<string> OnError;
    private RelayHostData _relayData;

    public void StartServer()
    {
        IsServer = true;
        // UnityEngine.Debug.Log("Start server. IsServer: " + NetworkManager.IsServer + " IsHost: " + NetworkManager.IsHost);
        StartCoroutine(StartAsServerCR());
    }
    public void StartClient(string code)
    {
        SessionCode = code;
        // UnityEngine.Debug.Log("Start client. Session code is now: " + code + " c: " + code.Length + " IsServer: " + NetworkManager.IsServer + " IsHost: " + NetworkManager.IsHost);
        StartCoroutine(StartAsClientCR());
    }

    // debug purposes, remove later
    [SerializeField] private Image _image;
    private void Update()
    {
        if ( NetworkManager != null && IsServer)
            _image.color = Color.green;
        else
            _image.color = Color.red;
    }

    private void StartGame()
    {
        // SceneManager.LoadSceneAsync(_gameScene);
        NetworkManager.SceneManager.LoadScene(_gameScene, LoadSceneMode.Single);
    }
    private void Start()
    {
        if ( _transport == null )
            _transport = GetComponent<UnityTransport>();

        if ( NetworkManager == null )
            NetworkManager = GetComponent<NetworkManager>();
    
        if ( _transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport )
            IsRelay = true;
    }

    private IEnumerator StartAsServerCR()
    {
        NetworkManager.enabled = true;
        _transport.enabled = true;

        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

        // Wait a frame for setups to be done
        yield return null;

        if ( IsRelay )
        {
            Task<bool> loginTask = Login();

            yield return new WaitUntil( () => loginTask.IsCompleted );

            if ( loginTask.Exception != null )
            {
                UnityEngine.Debug.Log("Login failed: " + loginTask.Exception);
                OnError.Invoke("Could not connect to Unity services. Please check your internet connection and try again.");
                yield break;
            }

            // create allocation here

            Task<Allocation> allocationTask = CreateAllocationData();

            yield return new WaitUntil(() => allocationTask.IsCompleted);

            if ( allocationTask.Exception != null )
            {
                UnityEngine.Debug.Log("Allocation failed: " + allocationTask.Exception);
                OnError.Invoke("Could not create a game session. Please check your connection or try again in a few moments.");
                yield break;
            }
            else
            {
                UnityEngine.Debug.Log("Allocation successful");
                Allocation allocation = allocationTask.Result;

                _relayData = new RelayHostData();

                // Find the first endpoint
                foreach( RelayServerEndpoint endpoint in allocation.ServerEndpoints )
                {
                    _relayData.IPv4Address = endpoint.Host;
                    _relayData.Port = (ushort) endpoint.Port;
                    break;
                }
                _relayData.AllocationID = allocation.AllocationId;
                _relayData.AllocationIDBytes = allocation.AllocationIdBytes;
                _relayData.ConnectionData = allocation.ConnectionData;
                _relayData.Key = allocation.Key;

                Task<string> joinCodeTask = GetJoinCodeAsync();

                yield return new WaitUntil( () => joinCodeTask.IsCompleted );

                if ( joinCodeTask.Exception != null )
                {
                    UnityEngine.Debug.Log("Join code failed: " + joinCodeTask.Exception);
                    OnError.Invoke("Unable to generate a session code. Please try again.");
                    yield break;
                }
                else
                {
                    UnityEngine.Debug.Log("Code retrieved. ");
                    _relayData.JoinCode = joinCodeTask.Result;
                    SessionCode = _relayData.JoinCode;

                    _transport.SetRelayServerData(
                        _relayData.IPv4Address,
                        _relayData.Port,
                        _relayData.AllocationIDBytes,
                        _relayData.Key,
                        _relayData.ConnectionData);
                }
            }

            UnityEngine.Debug.Log("Login successful. ");
            NetworkManager.StartHost();

            float timeout = 10f;
            while ( ! NetworkManager.IsHost && timeout > 0f )
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if ( ! NetworkManager.IsHost )
            {
                OnError.Invoke("Could not start server in time. ");
                yield break;
            }
            
            StartGame();
        }
    }

    private async Task<bool> Login()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if ( !AuthenticationService.Instance.IsSignedIn )
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch ( SystemException e )
        {
            UnityEngine.Debug.Log("Error login: " + e);
            throw;
        }

        return true;
    }

    public class RelayHostData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] HostConnectionData;
        public byte[] Key;
    }

    private async Task<Allocation> CreateAllocationData()
    {
        try
        {
            Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(_maxPlayers);
            return allocation;
        }
        catch ( SystemException e )
        {
            UnityEngine.Debug.Log("Error creating allocation: " + e);
            throw;
        }
    }

    private async Task<string> GetJoinCodeAsync()
    {
        try
        {
            string code = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(_relayData.AllocationID);
            return code;
        }
        catch ( SystemException e )
        {
            UnityEngine.Debug.Log("Error retrieving join code: " + e);
            throw;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if ( ! NetworkManager.IsServer ) return;

        UnityEngine.Debug.Log($"Player {clientId} connected!");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if ( ! NetworkManager.IsServer ) return;

        UnityEngine.Debug.Log($"Player {clientId} disconnected!");
    }

    private IEnumerator StartAsClientCR()
    {
        NetworkManager.enabled = true;
        _transport.enabled = true;
        // Wait a frame for setups to be done
        yield return null;

        if (IsRelay)
        {
            Task<bool> loginTask = Login();

            yield return new WaitUntil( () => loginTask.IsCompleted );

            if ( loginTask.Exception != null )
            {
                UnityEngine.Debug.LogError("Login failed: " + loginTask.Exception);
                OnError.Invoke("Could not connect to Unity services. Please check your internet connection and try again.");
                yield break;
            }

            Task<JoinAllocation> joinAllocationTask = JoinAllocationAsync();

            yield return new WaitUntil( () => joinAllocationTask.IsCompleted );

            if ( joinAllocationTask.Exception != null )
            {
                UnityEngine.Debug.Log("Join allocation failed: " + joinAllocationTask.Exception);
                OnError.Invoke("Could not join the game session. The session code may be invalid or the session is full.");
                yield break;
            }
            else
            {
                UnityEngine.Debug.Log("Allocation joined. ");

                _relayData = new RelayHostData();
                JoinAllocation allocation = joinAllocationTask.Result;

                foreach (RelayServerEndpoint endpoint in allocation.ServerEndpoints)
                {
                    _relayData.IPv4Address = endpoint.Host;
                    _relayData.Port = (ushort) endpoint.Port;
                    break;
                }
                _relayData.AllocationID = allocation.AllocationId;
                _relayData.AllocationIDBytes = allocation.AllocationIdBytes;
                _relayData.ConnectionData = allocation.ConnectionData;
                _relayData.HostConnectionData = allocation.HostConnectionData;
                _relayData.Key = allocation.Key;

                _transport.SetClientRelayData(
                    _relayData.IPv4Address,
                    _relayData.Port,
                    _relayData.AllocationIDBytes,
                    _relayData.Key,
                    _relayData.ConnectionData,
                    _relayData.HostConnectionData);
            }

            UnityEngine.Debug.Log("Login successful! ");
            NetworkManager.StartClient();

            UnityEngine.Debug.Log("Login IsServer: " + NetworkManager.IsServer + " IsHost: " + NetworkManager.IsHost);

            float timeout = 10f;
            while ( ! NetworkManager.IsConnectedClient && timeout > 0f )
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if ( ! NetworkManager.IsConnectedClient )
            {
                OnError.Invoke("Could not join server in time. ");
                // yield break;
            }

            // StartGame();
        }
    }

    private async Task<JoinAllocation> JoinAllocationAsync()
    {
        try
        {
            JoinAllocation allocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(SessionCode);
            return allocation;
        }
        catch ( SystemException e )
        {
            UnityEngine.Debug.Log("Error joining allocation: " + e);
            throw;
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Build and Launch")]
    public static void Build()
    {
        // Specify build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = EditorBuildSettings.scenes
          .Where(s => s.enabled)
          .Select(s => s.path)
          .ToArray();
        buildPlayerOptions.locationPathName = Path.Combine("Builds", "GarPic.apk");
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;
        // Perform the build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        // Output the result of the build
        UnityEngine.Debug.Log($"Build ended with status: {report.summary.result}");


        if ( report.summary.result != BuildResult.Succeeded ) return;

        Launch();
    }

    [MenuItem("Tools/Launch")]
    public static void Launch()
    {
        Process process = new Process();
        process.StartInfo.FileName = Path.Combine(Path.GetFullPath("Builds"), "BuildInstallLaunch.bat");
        process.StartInfo.WorkingDirectory = Path.GetFullPath("Builds");
        UnityEngine.Debug.Log("Starting process with filename: " + process.StartInfo.FileName + " at: " + process.StartInfo.WorkingDirectory);
        process.StartInfo.UseShellExecute = true;
        process.Start();
    }
#endif

}