using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GPSTimer : NetworkBehaviour
{
    [SerializeField] private float _baseRunTime = 20f;
    private TMP_Text _text;
    private float _runTime;
    private bool _GPSsuccess = false;
    private string _gpsError = "";

    public int Timer => _timer.Value;
    private NetworkVariable<int> _timer = new NetworkVariable<int>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        TimerDisplay display = FindFirstObjectByType<TimerDisplay>();
        display.AddPlayer(this);

        if ( IsOwner )
        {
            PhotoTaking photoTaking = FindFirstObjectByType<PhotoTaking>();
            photoTaking.SetGPS(this);

            StartCoroutine( StartGPS() );
            _text = display.TimeText;
            Debug.Log("text is null? " + _text == null);
        }

        _timer.Value = -1;
    }

    private IEnumerator StartGPS()
    {
        Debug.Log("PhotoTaking? Trying to start GPS. ");

        if ( ! IsOwner ) yield break;

        #if UNITY_EDITOR
        yield return new WaitUntil( () => UnityEditor.EditorApplication.isRemoteConnected);

        #elif UNITY_ANDROID

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation))
        {
            _gpsError = "GPS Requesting access";
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.CoarseLocation);
        }

        // First, check if user has location service enabled
        if ( ! Input.location.isEnabledByUser )
        {
            _gpsError = "GPS NOT ALLOWED";
        }

        yield return new WaitUntil( () => Input.location.isEnabledByUser &&
            UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation)); 
        
        #endif

        // Start service before querying location
        Input.location.Start(1f, 1f);
                
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
            _gpsError = "GPS TIMED OUT";
            yield break;
        }

        if ( Input.location.status == LocationServiceStatus.Running )
        {
            _GPSsuccess = true;
            _gpsError = "GPS ACCESS GRANTED";
        }
    }

    public IEnumerator StartCount()
    {
        Debug.Log("PhotoTaking? Trying to start Count: " + IsOwner);

        if ( ! IsOwner ) yield break;

        _stop = false;

        Debug.Log("PhotoTaking? Moving Starting Count. ");

        // wait until there is no movement
        WaitUntil waitTillNotMoving = new WaitUntil( () => !IsMoving() );
        
        _runTime = _baseRunTime;

        Debug.Log("Run time Moving. ");
        Debug.Log("Updating timer:  " +_runTime + " text null? " + _text == null );

        while ( _runTime > 0 )
        {
            _text.text = _runTime.ToString("F2");
            _runTime -= Time.deltaTime;

            int t2 = Mathf.FloorToInt(_runTime);
            if ( t2 != _timer.Value && t2 >= 0)
            {
                _timer.Value = t2;
            }

            Debug.Log("Updating timer:  " +_runTime + " text null? " + _text == null + " _timer? " + _timer.Value );

            // yield return waitTillNotMoving;

            if (_stop)
                break;
            
            yield return null;
        }

        _timer.Value = -1;
    }

    private bool _stop;

    public void StopCoroutine()
    {
        _stop = true;
    }

    private Vector2 _lastLocation;
    private bool IsMoving()
    {
        Debug.Log("Checking for Moving. ");

        if ( !_GPSsuccess )
        {
            _text.text += "\n" + _gpsError;
            return false;
        }

        float newLat = Mathf.Round(Input.location.lastData.latitude * 100000f) / 100000f;
        float newLon = Mathf.Round(Input.location.lastData.longitude * 100000f) / 100000f;

        if ( _lastLocation == null )
        {
            _lastLocation = new Vector2()
            {
                x = newLat,
                y = newLon
            };
        }

        // 1-5 meter accuracy?

        bool isMoving = Mathf.Abs(newLat - _lastLocation.x) > 0.00001f
                    || Mathf.Abs(newLon - _lastLocation.y) > 0.00001f;

        _text.text += "\n" + "n: " + newLat + " " + newLon + "\n" +
                "o: " + _lastLocation.x + " " + _lastLocation.y +  "\n" +
                " Moved? " + isMoving;

        Debug.Log( "n: " + newLat + " " + newLon + "\n" +
                "0: " + _lastLocation.x + " " + _lastLocation.y +  "\n" +
                " Moved? " + isMoving);

        _lastLocation.x = newLat;
        _lastLocation.y = newLon;

        Debug.Log("Moving? " + ! isMoving );

        return isMoving;
    }
}
