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
    
    public Dictionary<ushort, Tuple<int, int>> MovedItems;

    private void Awake()
    {
        TcpConfig tcpConfig = new TcpConfig(true, 5000, 20000);
        _webClient = SimpleWebClient.Create(16*1024, 1000, tcpConfig);
        _webClient.onConnect += WebClientOnonConnect;
        _webClient.onData += WebClientOnonData;
        Builder.ObjectMoved += BuilderOnObjectMoved;
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
                _currentState = (GameState)_bitBuffer.ReadByte();
                // Read all the ids and names it gives me and store it into _names to be used when players get spawned
                ushort count = _bitBuffer.ReadUShort();
                for (int i = 0; i < count; i++)
                {
                    ushort id = _bitBuffer.ReadUShort();
                    string name = _bitBuffer.ReadString();
                    _names[id] = name;
                }

                // Put my name into _names too for when I am the builder
                _names[_myId] = ConnectUIController.DisplayName;
                
                HandleStateChange(_currentState);
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
                        PositionInterp positionInterp = newPlayer.GetComponent<PositionInterp>();
                        positionInterp.enabled = true;
                        _otherPlayers[id] = positionInterp;
                        newPlayer.GetComponent<Nametag>().SetName(_names[id]);
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
                for (int i = 0; i < count; i++)
                {
                    ushort id = _bitBuffer.ReadUShort();
                    short points = _bitBuffer.ReadShort();
                    _points[id] = points;
                    
                    _otherPlayers[id].GetComponent<Nametag>().SetPts(points);
                }

                break;
            }
            case 9:
            {
                ushort id = _bitBuffer.ReadUShort();
                string name = _bitBuffer.ReadString();
                _names[id] = name;

                break;
            }
        }
    }

    private void WebClientOnonConnect()
    {
        Debug.Log("Client connected");
        
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
            
            // Send our position to server
            _bitBuffer.Clear();
            _bitBuffer.AddByte(1);

            QuantizedVector3 qPosition = BoundedRange.Quantize(LocalPlayerTransform.position, Constants.WORLD_BOUNDS);
            _bitBuffer.AddUInt(qPosition.x);
            _bitBuffer.AddUInt(qPosition.y);

            _bitBuffer.ToArray(_buffer);
            _webClient.Send(new ArraySegment<byte>(_buffer, 0, 9));
        }
    }

    private void OnDestroy()
    {
        _webClient.Disconnect();
    }

    private void HandleStateChange(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Waiting:
            {
                // Teleport to waiting room
                LocalPlayerTransform.position = WaitingRoomLocation.position;
                StatusText.enabled = true;
                StatusText.text = "Waiting for at least two players";
                Debug.Log("New state: Waiting");
                break;
            }
            case GameState.Begin:
            {
                // Tell people who the builder will be
                _builderId = _bitBuffer.ReadUShort();
                string name = _names[_builderId];
                StatusText.enabled = true;
                StatusText.text = "Next Shifter:\n" + name;
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
                    StatusText.text = "Waiting for Shifter:\n" + name;
                }
                
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
                
                Debug.Log("New state: Search");
                break;
            }
            case GameState.Scoring:
            {
                if (_myId != _builderId)
                {
                    ExitSearchMode();
                }
                // Teleport to waiting room
                LocalPlayerTransform.position = WaitingRoomLocation.position;
                
                Debug.Log("New state: Scoring");
                break;
            }
        }
    }

    private void EnterBuildingMode()
    {
        MovedItems = new Dictionary<ushort, Tuple<int, int>>();
        GetComponent<Builder>().enabled = true;
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
        // If we haven't moved this object before
        if (!MovedItems.ContainsKey(objectId))
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
    }

    private void UpdateBuilderStatusText()
    {
        StatusText.enabled = true;
        StatusText.text = "Objects moved: " + MovedItems.Count + "/" + Constants.MAX_SHIFTED_OBJECTS;
    }

    private void EnterSearchMode()
    {
        // Move the moved items to their new positions
        MoveableReferencer moveableReferencer = GetComponent<MoveableReferencer>();
        foreach (var movedItem in MovedItems)
        {
            GridItem gridItem = moveableReferencer.Moveables[movedItem.Key];
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
