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

    private bool isServer = false;

    void Start()
    {
        // Parse command line arguments
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--server")
            {
                // --server found, this should be a server application
                isServer = true;
            }
        }
        if (isServer)
            StartCoroutine(StartAsServerCR());
        else
            StartCoroutine(StartAsClientCR());
    }

    IEnumerator StartAsServerCR()
    {
        SetWindowTitle("Starting up as server...");
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;
        // Wait a frame for setups to be done
        yield return null;
        if (networkManager.StartServer())
        {
            SetWindowTitle("Server");
            UnityEngine.Debug.Log($"Serving on port {transport.ConnectionData.Port}...");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        }
        else
        {
            SetWindowTitle("Failed to connect as server...");
            UnityEngine.Debug.LogError($"Failed to serve on port {transport.ConnectionData.Port}...");
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
        SetWindowTitle("Starting up as client...");
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;
        // Wait a frame for setups to be done
        yield return null;
        if (networkManager.StartClient())
        {
            SetWindowTitle("Client");
            UnityEngine.Debug.Log($"Connecting on port {transport.ConnectionData.Port}...");
        }
        else
        {
            SetWindowTitle("Failed to connect as client...");
            UnityEngine.Debug.LogError($"Failed to connect on port {transport.ConnectionData.Port}...");
        }
    }

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowText(IntPtr hWnd, string lpString);
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    static extern IntPtr EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    // Delegate to filter windows
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private static IntPtr FindWindowByProcessId(uint processId)
    {
        IntPtr windowHandle = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            uint windowProcessId;
            GetWindowThreadProcessId(hWnd, out windowProcessId);
            if (windowProcessId == processId)
            {
                windowHandle = hWnd;
                return false; // Found the window, stop enumerating
            }
            return true; // Continue enumerating
        }, IntPtr.Zero);
        return windowHandle;
    }
    static void SetWindowTitle(string title)
    {
#if !UNITY_EDITOR
                uint processId = (uint)Process.GetCurrentProcess().Id;
                IntPtr hWnd = FindWindowByProcessId(processId);
                if (hWnd != IntPtr.Zero)
                {
                    SetWindowText(hWnd, title);
                }
#endif
        }
#else
        static void SetWindowTitle(string title)
        {
        }
#endif

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
        buildPlayerOptions.locationPathName = Path.Combine("Builds", "GarPic.exe");
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;
        // Perform the build
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        // Output the result of the build
        UnityEngine.Debug.Log($"Build ended with status: {report.summary.result}");


        if ( report.summary.result != BuildResult.Succeeded ) return;

        Process process = new Process();
        process.StartInfo.FileName = Path.Combine("Builds", "BuildInstallLaunch.bat");
        process.StartInfo.UseShellExecute = true;
        process.Start();
    }
#endif

}