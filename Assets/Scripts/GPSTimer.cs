using System.Collections;
using TMPro;
using UnityEngine;

public class GPSTimer : MonoBehaviour
{
    [SerializeField] private float _baseRunTime = 20f;
    [SerializeField] private TMP_Text _text;
    private float _runTime;
    private bool _GPSsuccess = false;
    private void Start()
    {
        StartCoroutine( StartGPS() );
    }

    private IEnumerator StartGPS()
    {
        #if UNITY_EDITOR
        yield return new WaitUntil( () => UnityEditor.EditorApplication.isRemoteConnected);

        #elif UNITY_ANDROID

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation))
        {
            _text.text = "Requesting access";
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.CoarseLocation);
        }

        // First, check if user has location service enabled
        if ( ! Input.location.isEnabledByUser )
        {
            _text.text = "Requesting access NOT ALLOWED";
        }

        yield return new WaitUntil( () => Input.location.isEnabledByUser &&
            UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation)); 

        _text.text = "";
        
        #endif

        // Start service before querying location
        Input.location.Start(5f, 5f);
                
        // Wait until service initializes
        int maxWait = 15;
        while ( Input.location.status == LocationServiceStatus.Initializing && maxWait > 0 )
        {
            yield return new WaitForSecondsRealtime(1);
            maxWait--;
        }

        // Editor has a bug which doesn't set the service status to Initializing. So extra wait in Editor.
        #if UNITY_EDITOR
                int editorMaxWait = 15;
                while ( Input.location.status == LocationServiceStatus.Stopped && editorMaxWait > 0 )
                {
                    yield return new WaitForSecondsRealtime(1);
                    editorMaxWait--;
                }
        #endif

        // Service didn't initialize in 15 seconds
        if ( maxWait < 1 )
        {
            Debug.Log("Moving Timed out");
            yield break;
        }

        if ( Input.location.status == LocationServiceStatus.Running )
            _GPSsuccess = true;
    }

    private void Update()
    {
        if ( Input.GetKeyDown(KeyCode.K))
            StartRun();
    }

    public void StartRun()
    {
        StartCoroutine( StartCount() );
    }

    private IEnumerator StartCount()
    {
        Debug.Log("Moving Starting Count. ");

        // wait until there is no movement
        WaitUntil waitTillNotMoving = new WaitUntil( () => !IsMoving() );
        
        _runTime = _baseRunTime;

        Debug.Log("Run time Moving. ");

        while ( _runTime > 0 )
        {
            Debug.Log("Running Moving and input location is: " + Input.location.isEnabledByUser);
            if ( _GPSsuccess )
                yield return waitTillNotMoving;

            _runTime -= Time.deltaTime;
            yield return null;

            _text.text = _runTime.ToString();
        }
        
        // warn somewhere that its done
    }

    private LocationInfo? _lastLocation;
    private bool IsMoving()
    {
        Debug.Log("Checking for Moving. ");

        if ( !_GPSsuccess )
            return false;

        if ( !_lastLocation.HasValue )
        {
            _lastLocation = Input.location.lastData;
            return false;
        }

        bool isEqual = Input.location.lastData.latitude == _lastLocation?.latitude
            && Input.location.lastData.longitude == _lastLocation?.longitude;
        _lastLocation = Input.location.lastData;

        _text.text += "\n" + Input.location.lastData.longitude.ToString() + " " + Input.location.lastData.latitude.ToString();

        Debug.Log("Moving? " + ! isEqual );

        return ! isEqual;
    }
}
