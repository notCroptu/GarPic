using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostJoin : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private NetworkSetup _networkSetup;
    [SerializeField] private GameObject _errorGO;
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private Button _Done;
    [SerializeField] private Button _Host;

    private void Start()
    {
        _networkSetup.OnError += GiveError;
        _inputField.onValueChanged.AddListener(OnInputChanged);

        if ( _networkSetup == null )
            _networkSetup = FindFirstObjectByType<NetworkSetup>();
    }

    private void OnInputChanged(string value)
    {
        string upper = value.ToUpper();
        if (upper == _inputField.text)
            return;
        _inputField.text = upper;
        _inputField.caretPosition = upper.Length;
    }

    public void StartClient()
    {
        if ( _networkSetup == null )
            _networkSetup = FindFirstObjectByType<NetworkSetup>();
        
        _networkSetup.StartClient(_inputField.text);
        _Done.interactable = false;
    }

    public void StartServer()
    {
        if ( _networkSetup == null )
            _networkSetup = FindFirstObjectByType<NetworkSetup>();
        
        _networkSetup.StartServer();
        _Host.interactable = false;
    }

    public void GiveError(string error)
    {
        StartCoroutine(Error(error));
        _Host.interactable = true;
        _Done.interactable = true;
    }

    private IEnumerator Error(string error)
    {
        _errorGO.SetActive(true);

        _errorText.text = error;
        _inputField.text = "";

        yield return new WaitForSeconds(4.5f);

        _errorGO.SetActive(false);
    }

    private void OnDisable()
    {
        if ( _networkSetup == null )
            _networkSetup.OnError -= GiveError;
    }
}
