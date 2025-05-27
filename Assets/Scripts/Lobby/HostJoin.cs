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

    private void Start()
    {
        _networkSetup.OnError += GiveError;
    }

    public void StartClient()
    {
        _networkSetup.StartClient(_inputField.text);
    }

    public void GiveError(string error)
    {
        StartCoroutine(Error(error));
    }

    private IEnumerator Error(string error)
    {
        _errorGO.SetActive(true);

        _errorText.text = error;
        _inputField.text = "";

        yield return new WaitForSeconds(2f);

        _errorGO.SetActive(false);
    }

    private void OnDisable()
    {
        _networkSetup.OnError -= GiveError;
    }
}
