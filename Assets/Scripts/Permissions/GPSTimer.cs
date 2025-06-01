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
            Debug.Log("Moving Timed out");
            yield break;
        }

        if ( Input.location.status == LocationServiceStatus.Running )
            _GPSsuccess = true;
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
                _timer.Value = t2;

            Debug.Log("Updating timer:  " +_runTime + " text null? " + _text == null + " _timer? " + _timer.Value );

            if ( _GPSsuccess )
                yield return waitTillNotMoving;

            if (_stop)
                break;
            
            yield return null;
        }

        _timer.Value = 0;
    }

    private bool _stop;

    public void StopCoroutine()
    {
        _stop = true;
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
