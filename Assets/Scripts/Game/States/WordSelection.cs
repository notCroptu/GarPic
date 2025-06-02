using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class WordSelection : GameState
{
    public string Word => _word.Value.ToString();
    private NetworkVariable<FixedString32Bytes> _word = new();
    private NetworkVariable<int> _timer = new();
    [SerializeField] private TMP_Text _wordTMP;
    [SerializeField] private TMP_Text _timerTMP;
    [SerializeField] private GameObject _canvas;

    private static readonly string[] _topics = 
        { "object", "emotion", "scene", "action", "place", "animal" };

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn IsServer: " + IsServer );

        base.OnNetworkSpawn();

        _word ??= new();
        _timer ??= new();

        _word.OnValueChanged += (_, _) => UpdateWord();
        _timer.OnValueChanged += (_, _) => UpdateTimer();

        _timer.Value = -1;
    }

    private void Start()
    {
        ResetValues();
    }

    public override IEnumerator State()
    {
        yield return base.State();

        string topic = _topics[Random.Range(0, _topics.Length)];
        string url = $"https://api.datamuse.com/words?rel_jja={topic}&max=1";
        using UnityWebRequest www = UnityWebRequest.Get(url);

        do
        {
            Debug.Log("WordSelection-ing. ");
            yield return www.SendWebRequest();
        } while (www.result != UnityWebRequest.Result.Success);

        string json = www.downloadHandler.text;
        
        int start = json.IndexOf("\"word\":\"") + 8;
        int end = json.IndexOf("\"", start);
        _word.Value = new FixedString32Bytes( json[start..end]);

        Debug.Log("done WordSelection: " + www.downloadHandler.text + " to word: " + Word);

        // start countdown

        float t = 10f;
        while ( t > 0f )
        {
            t -= Time.deltaTime;

            int t2 = Mathf.FloorToInt(t);
            if ( t2 != _timer.Value && t2 >= 0)
                _timer.Value = t2;
        
            yield return null;
        }

        _timer.Value = -1;
    }

    private void UpdateWord()
    {
        // update word on UI
        if ( ! _wordTMP.gameObject.activeSelf )
            _wordTMP.gameObject.SetActive(true);

        _wordTMP.text = Word;
    }

    private void UpdateTimer()
    {
        if ( _timer.Value == -1 )
            _timerTMP.text = "";
        else
            _timerTMP.text = _timer.Value.ToString();
    }

    public override void ResetValues()
    {
        base.ResetValues();

        _wordTMP.gameObject.SetActive(false);
        _timerTMP.gameObject.SetActive(true);
        _canvas.SetActive(true);

        Debug.Log("Start up word selection. Networks: " + _word.Value + " " + _timer.Value);
    }
}
