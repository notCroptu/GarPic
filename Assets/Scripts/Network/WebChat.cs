using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WebChat : NetworkBehaviour
{
    [SerializeField] private TMP_Text _messages;
    [SerializeField] private SessionStart _sessionStart;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _send;
    [SerializeField] private Button _slide;
    [SerializeField] private RectTransform _chat;
    [SerializeField] private GameObject _notification;
    [SerializeField] private ScrollRect _scrollRect;
    private bool _chatOn = false;
    private float _defaultY = float.MaxValue;

    private void Start()
    {
        _notification.SetActive(false);
        _send.onClick.AddListener(SendMessage);
        _slide.onClick.AddListener(ToggleChat);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if ( IsServer || IsHost )
        {
        _sessionStart.LoginPlayer += AddPlayer;
        _sessionStart.LogoutPlayer += RemovePlayer;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if ( IsServer || IsHost )
        {
             _sessionStart.LoginPlayer -= AddPlayer;
             _sessionStart.LogoutPlayer -= RemovePlayer;
        }
    }

    public void AddPlayer(string player)
    {
        if ( ! IsServer ) return;

        string message = "<b>" + player + "</b> has joined the session.";
        SendMessageServerRpc(message);
    }

    public void RemovePlayer(string player)
    {
        if ( ! IsServer ) return;

        string message = "<b>" + player + "</b> has left the session.";
        SendMessageServerRpc(message);
    }

    public void SendMessage()
    {
        if ( _inputField.text == "" ) return;

        string newMessage = "<b>" + _sessionStart.ClientNick + ":</b> " + _inputField.text;
        _inputField.text = "";

        SendMessageServerRpc(newMessage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string message)
    {
        ReceiveMessageClientRpc(message);
    }

    [ClientRpc]
    private void ReceiveMessageClientRpc(string message)
    {
        AddMessage(message);
    }

    private void AddMessage(string message)
    {
        if ( !_chatOn )
            _notification.SetActive(true);

        if (string.IsNullOrEmpty(_messages.text))
            _messages.text = message;
        else
            _messages.text += "\n" + message;
        
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private Coroutine _coroutine;
    public void ToggleChat()
    {
        if ( _defaultY == float.MaxValue )
        {
            _defaultY = _chat.anchoredPosition.y;
            Debug.Log("Set default y in webchat: " + _defaultY);
        }

        _notification.SetActive(false);

        if ( _coroutine != null )
            StopCoroutine(_coroutine);

        _chatOn = !_chatOn;

        _coroutine = StartCoroutine(Slide(_chatOn));
    }

    private IEnumerator Slide(bool goUp)
    {
        Vector2 startPos = _chat.anchoredPosition;
        Vector2 targetPos = startPos;
        targetPos.y = goUp ? 0f : _defaultY;

        float t = 0f, duration = 0.2f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _chat.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }

        _chat.anchoredPosition = targetPos;
    }
}
