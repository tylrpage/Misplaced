using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror.SimpleWeb;
using NetStack.Quantization;
using NetStack.Serialization;
using TMPro;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class Client : MonoBehaviour
{
    [SerializeField] private Transform LocalPlayerTransform;
    [SerializeField] private GameObject OtherPlayerPrefab;
    [SerializeField] private Transform WaitingRoomLocation;
    [SerializeField] private Transform MainRoomLocation;
    [SerializeField] private ConnectUIController ConnectUIController;
    [SerializeField] private TMP_Text StatusText;
    [SerializeField] private AudioClip ExplodeSound;

    public static event Action EnteringBuildingMode;
    public static event Action ExitingBuildingMode;

    private SimpleWebClient _webClient;
    private BitBuffer _bitBuffer = new BitBuffer(1024);
    private byte[] _buffer = new byte[1024];
    private Dictionary<ushort, PositionInterp> _otherPlayers = new Dictionary<ushort, PositionInterp>();
    private Dictionary<ushort, string> _names = new Dictionary<ushort, string>();
    private Dictionary<ushort, short> _points = new Dictionary<ushort, short>();
    private ushort _myId;
    private bool _handShakeComplete = false;
    private float _timeToSendNextUpdate = 0;
    private GameState _currentState;
    private ushort _builderId;
    private MoveableReferencer _moveableReferencer;
    private TimerTextController _timerTextController;
    private ScoreboardController _scoreboardController;
    private bool _wasHereForStartOfRound = false;
    private AudioSource _audioSource;
    private Vector3 _previousPlayerPosition = Vector3.zero;
    private bool _overrideDirtySendRule = false;
    private bool _wasConnected = false;
    private MusicController _musicController;
    
    public Dictionary<ushort, Tuple<int, int>> MovedItems;

    private void Awake()
    {
        _moveableReferencer = GetComponent<MoveableReferencer>();
        _timerTextController = GetComponent<TimerTextController>();
        _scoreboardController = GetComponent<ScoreboardController>();
        _audioSource = GetComponent<AudioSource>();
        _musicController = GetComponentInChildren<MusicController>();
        
        TcpConfig tcpConfig = new TcpConfig(true, 5000, 45000);
        _webClient = SimpleWebClient.Create(16*1024, 1000, tcpConfig);
        _webClient.onConnect += WebClientOnonConnect;
        _webClient.onData += WebClientOnonData;
        _webClient.onDisconnect += WebClientOnonDisconnect;
        Builder.ObjectMoved += BuilderOnObjectMoved;
        Interacter.WrongGuessMade += InteracterOnWrongGuessMade;
    }

    private void WebClientOnonDisconnect()
    {
        StatusText.enabled = true;
        if (_wasConnected)
        {
            StatusText.text = "You were disconnected. The AFK timer is 45s. Refresh to rejoin.";
        }
        else
        {
            StatusText.text = "Could not reach server";
        }

        _wasConnected = false;
    }

    public void Connect()
    {
        // connect
        UriBuilder uriBuilder = new UriBuilder()
        {
            Scheme = "wss",
            Host = "tylrpage.com",
            Port = Constants.GAME_PORT
        };
        _webClient.Connect(uriBuilder.Uri);
    }

    private void WebClientOnonData(ArraySegment<byte> data)
    {
        _bitBuffer.Clear();
        _bitBuffer.FromArray(data.Array, data.Count);
        byte messageId = _bitBuffer.ReadByte();
        
        switch (messageId)
        {
            case 2:
            {
                _myId = _bitBuffer.ReadUShort();
                // Put my name into _names too
                _names[_myId] = ConnectUIController.DisplayName;
                
                _currentState = (GameState)_bitBuffer.ReadByte();
                // Read all the ids and names it gives me and store it into _names to be used when players get spawned
                ushort count = _bitBuffer.ReadUShort();
                _scoreboardController.ResetScores();
                for (int i = 0; i < count; i++)
                {
                    ushort id = _bitBuffer.ReadUShort();
                    string name = _bitBuffer.ReadString();
                    short score = _bitBuffer.ReadShort();
                    _names[id] = name;
                    
                    Debug.Log("id: " + id + ", name: " + name);
                    
                    _scoreboardController.UpdateEntry(id, name, score);
                }
                _scoreboardController.DrawBoard();

                StatusText.enabled = true;
                if (_currentState == GameState.Waiting)
                {
                    StatusText.text = "Waiting for at least two players";
                }
                else
                {
                    StatusText.text = "Waiting for current round to end...";
                    _musicController.Pause();
                }

                _handShakeComplete = true;
                break;
            }
            case 3:
            {
                // GUARD FROM SETTING PLAYER POSITIONS UNTIL WE HAVE OUR ID
                if (!_handShakeComplete) break;
                
                ushort count = _bitBuffer.ReadUShort();
                for (int i = 0; i < count; i++)
                {
                    ushort id = _bitBuffer.ReadUShort();
                    uint qX = _bitBuffer.ReadUInt();
                    uint qY = _bitBuffer.ReadUInt();
                    
                    // GUARD FROM CHANGING LOCAL PLAYERS POSITION
                    if (id == _myId)
                        continue;
                    
                    QuantizedVector2 qPosition = new QuantizedVector2(qX, qY);
                    Vector2 position = BoundedRange.Dequantize(qPosition, Constants.WORLD_BOUNDS);

                    if (!_otherPlayers.ContainsKey(id))
                    {
                        // Create new player
                        GameObject newPlayer = Instantiate(OtherPlayerPrefab, position, Quaternion.identity);
                        Destroy(newPlayer.GetComponent<Rigidbody2D>());
                        Destroy(newPlayer.GetComponent<CircleCollider2D>());
                        Destroy(newPlayer.GetComponent<PlayerController>());
                        Destroy(newPlayer.GetComponent<MusicController>());
                        PositionInterp positionInterp = newPlayer.GetComponent<PositionInterp>();
                        positionInterp.enabled = true;
                        _otherPlayers[id] = positionInterp;
                        newPlayer.GetComponent<Nametag>().SetName(_names[id]);
                        _overrideDirtySendRule = true;
                    }
                    // Update the other players position
                    _otherPlayers[id].PushNewPosition(position);
                }
                break;
            }
            case 4:
            {
                ushort id = _bitBuffer.ReadUShort();
                if (_otherPlayers.ContainsKey(id))
                {
                    Destroy(_otherPlayers[id].gameObject);
                    _otherPlayers.Remove(id);
                }
                
                _scoreboardController.RemoveEntry(id);
                _scoreboardController.DrawBoard();

                break;
            }
            case 5:
            {
                _currentState = (GameState)_bitBuffer.ReadByte();
                HandleStateChange(_currentState);
                
                break;
            }
            case 7:
            {
                ushort count = _bitBuffer.ReadUShort();
                _scoreboardController.ResetScores();
                for (int i = 0; i < count; i++)
                {
                    ushort id = _bitBuffer.ReadUShort();
                    short points = _bitBuffer.ReadShort();

                    _scoreboardController.UpdateEntry(id, _names[id], points);
                }
                _scoreboardController.DrawBoard();

                break;
            }
            case 9:
            {
                ushort id = _bitBuffer.ReadUShort();
                string name = _bitBuffer.ReadString();
                _names[id] = name;
                
                // Add this new guy to the scoreboard
                _scoreboardController.UpdateEntry(id, name, 0);
                _scoreboardController.DrawBoard();

                break;
            }
            case 13:
            {
                ushort id = _bitBuffer.ReadUShort();
                if (_myId != id)
                {
                    _otherPlayers[id].GetComponentInChildren<Animator>().Play("girl_explode");
                    _audioSource.PlayOneShot(ExplodeSound);
                }

                break;
            }
        }
    }

    private void WebClientOnonConnect()
    {
        Debug.Log("Client connected");
        _wasConnected = true;
        
        // Hide the connect screen
        ConnectUIController.HideConnectScreen();
        
        // Setup your own nametag
        LocalPlayerTransform.GetComponent<Nametag>().SetName(ConnectUIController.DisplayName);
        
        // Send server your name and potentially some character customization stuff
        _bitBuffer.Clear();
        _bitBuffer.AddByte(8);
        _bitBuffer.AddString(ConnectUIController.DisplayName);
        _bitBuffer.ToArray(_buffer);
        _webClient.Send(new ArraySegment<byte>(_buffer, 0, 22));
    }

    private void LateUpdate()
    {
        _webClient.ProcessMessageQueue(this);
        
        if (_webClient.ConnectionState == ClientState.Connected && Time.time >= _timeToSendNextUpdate)
        {
            _timeToSendNextUpdate = Time.time + (1f / Constants.CLIENT_TICKRATE);

            // GUARD, DONT SEND POSITION IF WE DIDN'T MOVE
            if (!_overrideDirtySendRule && (_previousPlayerPosition - LocalPlayerTransform.position).magnitude < 0.1f)
                return;


            // Send our position to server
            _bitBuffer.Clear();
            _bitBuffer.AddByte(1);

            QuantizedVector3 qPosition = BoundedRange.Quantize(LocalPlayerTransform.position, Constants.WORLD_BOUNDS);
            _bitBuffer.AddUInt(qPosition.x);
            _bitBuffer.AddUInt(qPosition.y);

            _bitBuffer.ToArray(_buffer);
            _webClient.Send(new ArraySegment<byte>(_buffer, 0, 9));

            _previousPlayerPosition = LocalPlayerTransform.position;
            _overrideDirtySendRule = false;
        }
    }

    private void OnDestroy()
    {
        _webClient.Disconnect();
    }

    private void HandleStateChange(GameState gameState)
    {
        // GUARD, DON'T DO BUILD OR SEARCH PHASE IF YOU JOINED IN THE MIDDLE
        if (!_wasHereForStartOfRound && (gameState == GameState.Builder || gameState == GameState.Search))
            return;
        
        switch (gameState)
        {
            case GameState.Waiting:
            {
                // Teleport to waiting room
                LocalPlayerTransform.position = WaitingRoomLocation.position;
                StatusText.enabled = true;
                StatusText.text = "Waiting for at least two players";
                _timerTextController.HideTimer();
                _musicController.Pause();
                Debug.Log("New state: Waiting");
                break;
            }
            case GameState.Begin:
            {
                _wasHereForStartOfRound = true;
                
                // Tell people who the builder will be
                _builderId = _bitBuffer.ReadUShort();
                string name = _names[_builderId];
                StatusText.enabled = true;
                StatusText.text = "Next Shifter: " + name;
                _timerTextController.HideTimer();
                _musicController.Pause();
                Debug.Log("New state: Begin");
                break;
            }
            case GameState.Builder:
            {
                // If we are builder, enter builder mode
                if (_myId == _builderId)
                {
                    EnterBuildingMode();
                }
                else
                {
                    string name = _names[_builderId];
                    StatusText.enabled = true;
                    StatusText.text = "Waiting for Shifter: " + name;
                }
                
                _timerTextController.SetTimer("Shifting", Constants.SECONDS_WAITING_IN_BUILD);
                
                Debug.Log("New state: Builder");
                break;
            }
            case GameState.Search:
            {
                MovedItems = new Dictionary<ushort, Tuple<int, int>>();
                int count = _bitBuffer.ReadUShort();
                for (int i = 0; i < count; i++)
                {
                    ushort objectId = _bitBuffer.ReadUShort();
                    ushort newX = _bitBuffer.ReadUShort();
                    ushort newY = _bitBuffer.ReadUShort();
                    MovedItems[objectId] = new Tuple<int, int>(newX, newY);
                }

                // If we are not builder, enter searching mode
                if (_myId == _builderId)
                {
                    ExitBuildingMode();
                }
                else
                {
                    EnterSearchMode();
                }
                
                _timerTextController.SetTimer("Searching", Constants.SECONDS_WAITING_IN_SEARCH);
                _musicController.Play();
                
                Debug.Log("New state: Search");
                break;
            }
            case GameState.Scoring:
            {
                // Reset all items and outlines
                _moveableReferencer.ResetMoveables();
                
                if (_myId != _builderId)
                {
                    ExitSearchMode();
                }
                // Teleport to waiting room
                LocalPlayerTransform.position = WaitingRoomLocation.position;
                // Let player walk again incase they exploded
                LocalPlayerTransform.GetComponent<PlayerController>().enabled = true;

                StatusText.enabled = true;
                StatusText.text = "Generating scores...";
                _timerTextController.SetTimer("Scoring", Constants.SECONDS_WAITING_IN_SCORING);
                _musicController.Pause();
                
                Debug.Log("New state: Scoring");
                break;
            }
        }
    }

    private void EnterBuildingMode()
    {
        MovedItems = new Dictionary<ushort, Tuple<int, int>>();
        Builder builder = GetComponent<Builder>();
        builder.enabled = true;
        builder.Reset();
        EnteringBuildingMode?.Invoke();
        LocalPlayerTransform.position = MainRoomLocation.position;
        UpdateBuilderStatusText();
    }

    private void ExitBuildingMode()
    {
        GetComponent<Builder>().enabled = false;
        ExitingBuildingMode?.Invoke();
        StatusText.enabled = true;
        StatusText.text = "Waiting for other players to spot the difference";
    }
    
    private void BuilderOnObjectMoved(ushort objectId, int newX, int newY)
    {
        MovedItems[objectId] = new Tuple<int, int>(newX, newY);
        UpdateBuilderStatusText();

        // Tell server we moved an object
        _bitBuffer.Clear();
        _bitBuffer.AddByte(10);
        _bitBuffer.AddUShort(objectId);
        _bitBuffer.AddUShort((ushort)newX);
        _bitBuffer.AddUShort((ushort)newY);
        _bitBuffer.ToArray(_buffer);
        _webClient.Send(new ArraySegment<byte>(_buffer, 0, 13));
    }

    private void UpdateBuilderStatusText()
    {
        StatusText.enabled = true;
        StatusText.text = "Objects moved: " + MovedItems.Count + "/" + Constants.MAX_SHIFTED_OBJECTS;
    }

    private void EnterSearchMode()
    {
        // Move the moved items to their new positions
        foreach (var movedItem in MovedItems)
        {
            GridItem gridItem = _moveableReferencer.Moveables[movedItem.Key];
            gridItem.MoveToGridPos((ushort)movedItem.Value.Item1, (ushort)movedItem.Value.Item2);
        }
        
        Interacter interacter = GetComponent<Interacter>();
        interacter.enabled = true;
        interacter.Reset();
        
        LocalPlayerTransform.position = MainRoomLocation.position;
    }

    public bool IsMyGuessCorrect(ushort guessId)
    {
        return MovedItems.ContainsKey(guessId);
    }
    
    private void InteracterOnWrongGuessMade()
    {
        byte[] data = new byte[] {12};
        _webClient.Send(new ArraySegment<byte>(data, 0, 1));
    }

    private void ExitSearchMode()
    {
        Interacter interacter = GetComponent<Interacter>();
        
        // Tell server your points change
        int pointChange = interacter.CorrectItems - interacter.WrongItems;
        _bitBuffer.Clear();
        _bitBuffer.AddByte(11);
        _bitBuffer.AddShort((short) pointChange);
        _bitBuffer.ToArray(_buffer);
        _webClient.Send(new ArraySegment<byte>(_buffer, 0, 3));
        
        interacter.enabled = false;
    }
}
