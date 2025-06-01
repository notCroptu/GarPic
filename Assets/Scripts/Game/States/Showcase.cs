using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Showcase : GameState
{
    [SerializeField] private SessionStart _sessionStart;
    [SerializeField] private GameLoop _gameloop;
    [SerializeField] private GameObject _canvas;
    [SerializeField] private TMP_Text _timerTMP;
    [SerializeField] private TMP_Text _playerNick;
    [SerializeField] private RawImage _display;
    [SerializeField] private Texture2D _nullTexture;

    private NetworkVariable<int> _timer = new();

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn IsServer: " + IsServer );

        base.OnNetworkSpawn();

        _timer ??= new();

        _timer.OnValueChanged += (_, _) => UpdateTimer();
    }

    private void Start()
    {
        _canvas.SetActive(false);
    }

    public override IEnumerator State()
    {
        yield return base.State();

        foreach ( ulong id in _networkSetup.NetworkManager.ConnectedClientsIds)
        {
            // server checks which players took a photo with supa

            string url = BuildPhotoUrl(id);

            // iterates through them, tells the clients to find that photo and show it and the player nickname

            int maxTries = 3;
            int attempt = 0;
            bool success = false;

            while ( attempt < maxTries && !success )
            {
                using (UnityWebRequest www = UnityWebRequest.Head(url))
                {
                    www.SetRequestHeader("apikey", LoadSupabaseConfig.Config.supabaseANON);
                    www.SetRequestHeader("Authorization", $"Bearer {LoadSupabaseConfig.Config.supabaseANON}");
                    www.SetRequestHeader("Content-Type", "image/png");

                    Debug.Log($"Header attempt {attempt + 1}");

                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        success = true;
                        Debug.Log("Url exists!");

                        UpdatePictureClientRpc(id);

                        // waits a a server side countdown

                        float t = 10f;
                        while ( t > 0f )
                        {
                            t -= Time.deltaTime;

                            int t2 = Mathf.FloorToInt(t);
                            if ( t2 != _timer.Value && t2 >= 0)
                                _timer.Value = t2;
                        
                            yield return null;
                        }
                        // continues to next client
                        break;
                    }
                    else
                    {
                        Debug.LogWarning("Couldn't find: " + www.error);
                    }
                }
                attempt++;
            }
        }

        DisableShowcaseClientRpc();
    }

    [ClientRpc]
    public void UpdatePictureClientRpc(ulong clientId)
    {
        StartCoroutine(UpdatePicture(clientId));
    }

    private IEnumerator UpdatePicture(ulong clientId)
    {
        if ( !_canvas.activeSelf )
            _canvas.SetActive(true);

        string url = BuildPhotoUrl(clientId);

        _playerNick.text = _sessionStart.FindNickname(clientId);

        int maxTries = 3;
        int attempt = 0;
        bool success = false;
        byte[] data = null;

        while (attempt < maxTries && !success)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("apikey", LoadSupabaseConfig.Config.supabaseANON);
                www.SetRequestHeader("Authorization", $"Bearer {LoadSupabaseConfig.Config.supabaseANON}");
                www.SetRequestHeader("Content-Type", "image/png");

                Debug.Log("Photo get attempt " + (attempt + 1));

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    success = true;
                    data = www.downloadHandler.data;
                    Debug.Log("done getting photo");
                }
                else
                {
                    Debug.Log("Photo get failed: " + www.error);
                }
            }
            attempt++;
        }

        if ( !success || data == null )
        {
            Debug.Log("Failed to download photo for: " + clientId);
            _display.texture = _nullTexture;
            yield break;
        }

        Texture2D photo = new Texture2D(2, 2);

        if ( !photo.LoadImage(data) )
        {
            Debug.Log("Failed to decode downloaded image for: " + clientId);
            _display.texture = _nullTexture;
            yield break;
        }

        Debug.Log("done getting photo for : " + clientId);

        _display.texture = photo;
    }

    private void UpdateTimer()
    {
        if ( ! _timerTMP.gameObject.activeSelf )
            _timerTMP.gameObject.SetActive(true);

        _timerTMP.text = _timer.Value.ToString();
    }

    [ClientRpc]
    public void DisableShowcaseClientRpc()
    {
        _canvas.SetActive(false);
        _display.texture = null;
        _playerNick.text = "";
        _timerTMP.text = "";
    }

    private string BuildPhotoUrl(ulong id) =>
        $"{LoadSupabaseConfig.Config.supabaseURL}/storage/v1/object/{LoadSupabaseConfig.Config.supabaseBUCKET}/" +
        $"{_networkSetup.SessionCode}/{id}/photo_{_gameloop.Round.Value}.png";
}
