using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PhotoTaking : GameState
{
    [SerializeField] private RawImage _display;
    [SerializeField] private Button _takePhoto;
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GPSTimer _timer;
    private WebCamTexture _webcamTexture;

    private int _photoNum = 0;
    private byte[] _imageBytes;
    private Texture2D _photo;

    private IEnumerator DelayedCameraInitialization()
    {
        yield return null;

        yield return AskCameraPermission();

        InitializeCamera();
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
        _webcamTexture = new WebCamTexture();
        _display.color = Color.white;
        _display.texture = _webcamTexture;
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
        if ( _webcamTexture == null || !_webcamTexture.isPlaying )
            return;

        _photo = new Texture2D(_webcamTexture.width, _webcamTexture.height);
        _photo.SetPixels32(_webcamTexture.GetPixels32());
        _photo.Apply();
        _imageBytes = _photo.EncodeToPNG();
    }

    public override IEnumerator State()
    {
        _canvas.SetActive(true);
        _takePhoto.gameObject.SetActive(true);

        yield return DelayedCameraInitialization();

        // wait until count is done
        yield return _timer.StartCount();

        _takePhoto.gameObject.SetActive(false);
        _webcamTexture.Stop();
        _display.texture = _photo;

        yield return UploadPhoto();
        
        _canvas.SetActive(false);
        _imageBytes = null;
    }

    public IEnumerator UploadPhoto()
    {
        if ( LoadSupabaseConfig.Config == null )
        {
            Debug.LogError("PhotoTaking supabase config file not found. ");
            yield break;
        }

        if (_imageBytes == null)
        {
            Debug.LogError("PhotoTaking no photo to upload!");
            yield break;
        }

        _photoNum++;
        string fileName = $"{NetworkManager.ServerClientId}/{NetworkManager.Singleton.LocalClientId}/photo{_photoNum}.png";
        string url = $"{LoadSupabaseConfig.Config.supabaseURL}/storage/v1/object/{LoadSupabaseConfig.Config.supabaseBUCKET}/{fileName}";
        // TODO: add client id and session to file hiererchy name for id

        UnityWebRequest www = UnityWebRequest.Put(url, _imageBytes);
        www.SetRequestHeader("apikey", LoadSupabaseConfig.Config.supabaseANON);
        www.SetRequestHeader("Authorization", $"Bearer {LoadSupabaseConfig.Config.supabaseANON}");
        www.SetRequestHeader("Content-Type", "image/png");

        do
        {
            Debug.Log("PhotoTaking-ing. ");
            yield return www.SendWebRequest();
        } while (www.result != UnityWebRequest.Result.Success);

        Debug.Log("done PhotoTaking");
    }
}