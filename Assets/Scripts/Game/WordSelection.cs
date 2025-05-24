using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class WordSelection : GameState
{
    [field:SerializeField] public string Word { get; private set; }
    [SerializeField] private TMP_Text _word;
    [SerializeField] private GameObject _canvas;

    private string[] _topics = { "object", "emotion", "scene", "action", "place", "animal" };

    public override IEnumerator State()
    {
        _canvas.SetActive(true);

        Debug.Log("starting WordSelection. ");

        string url = "https://api.datamuse.com/words?rel_jja=" + _topics[ Random.Range(0, _topics.Length) ] + "&max=1";
        using UnityWebRequest www = UnityWebRequest.Get(url);
        
        Word = null;

        do
        {
            Debug.Log("WordSelection-ing. ");
            yield return www.SendWebRequest();
        } while (www.result != UnityWebRequest.Result.Success);

        string json = www.downloadHandler.text;
        
        int start = json.IndexOf("\"word\":\"") + 8;
        int end = json.IndexOf("\"", start);
        Word = json[start..end];

        _word.text = Word;

        Debug.Log("done WordSelection: " + www.downloadHandler.text + " to word: " + Word);

        _canvas.SetActive(false);
    }
}
