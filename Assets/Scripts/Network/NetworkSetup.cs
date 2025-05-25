using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Diagnostics;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;






#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;

#endif

#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif

public class NetworkSetup : MonoBehaviour
{
    private List<ulong> _clientIDs;
    [SerializeField] private UnityTransport _transport;
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private int _maxPlayers;
    public bool IsRelay { get; private set; } = false;
    public bool IsServer  { get; private set; } = false;
    private string _session;
    public string Session
    {
        get { return _session; }
        set {
            _session = value;
            OnSetSession.Invoke();
        }
    }
    public Action OnSetSession;

    void Start()
    {
        // Parse command line arguments
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--server")
            {
                // --server found, this should be a server application
                IsServer = true;
            }
        }

        if ( _transport == null )
            _transport = GetComponent<UnityTransport>();

        if ( _networkManager == null )
            _networkManager = GetComponent<NetworkManager>();
    
        if ( _transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport )
            IsRelay = true;

        if (IsServer)
            StartCoroutine(StartAsServerCR());
        else
            StartCoroutine(StartAsClientCR());
    }

    IEnumerator StartAsServerCR()
    {
        _networkManager.enabled = true;
        _transport.enabled = true;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Wait a frame for setups to be done
        yield return null;

        if ( IsRelay )
        {
            Task<bool> loginTask = Login();

            yield return new WaitUntil( () => loginTask.IsCompleted );

            if ( loginTask.Exception != null )
            {
                UnityEngine.Debug.Log("Login failed: " + loginTask.Exception);
                yield break;
            }

            UnityEngine.Debug.Log("Login successful. ");
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

    private void OnClientConnected(ulong clientId)
    {
        if ( ! NetworkManager.Singleton.IsServer ) return;

        UnityEngine.Debug.Log($"Player {clientId} connected!");

        _clientIDs.Add(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if ( ! NetworkManager.Singleton.IsServer ) return;

        UnityEngine.Debug.Log($"Player {clientId} disconnected!");

        _clientIDs.Remove(clientId);
    }

    IEnumerator StartAsClientCR()
    {
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;
        // Wait a frame for setups to be done
        yield return null;
        if (networkManager.StartClient())
        {
            UnityEngine.Debug.Log($"Connecting on port {transport.ConnectionData.Port}...");
        }
        else
        {
            UnityEngine.Debug.LogError($"Failed to connect on port {transport.ConnectionData.Port}...");
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Build and Launch")]
    public static void BuildLaunch()
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
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        // Output the result of the build
        UnityEngine.Debug.Log($"Build ended with status: {report.summary.result}");


        if ( report.summary.result != BuildResult.Succeeded ) return;

        Process process = new Process();
        process.StartInfo.FileName = Path.Combine("Builds", "BuildInstallLaunch.bat");
        process.StartInfo.WorkingDirectory = Path.GetFullPath("Builds");
        process.StartInfo.UseShellExecute = true;
        process.Start();
    }
#endif

}