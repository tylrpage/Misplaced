using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror.SimpleWeb;
using NetStack.Quantization;
using NetStack.Serialization;

public class Client : MonoBehaviour
{
    [SerializeField] private Transform LocalPlayerTransform;
    [SerializeField] private GameObject OtherPlayerPrefab;
    
    private SimpleWebClient _webClient;
    private BitBuffer _bitBuffer = new BitBuffer(1024);
    private byte[] _buffer = new byte[1024];
    private Dictionary<ushort, PositionInterp> _otherPlayers = new Dictionary<ushort, PositionInterp>();
    private ushort _myId;
    private bool _handShakeComplete = false;
    private float _timeToSendNextUpdate = 0;
    private GameState _currentState;

    private void Awake()
    {
        TcpConfig tcpConfig = new TcpConfig(true, 5000, 20000);
        _webClient = SimpleWebClient.Create(16*1024, 1000, tcpConfig);
        _webClient.onConnect += WebClientOnonConnect;
        _webClient.onData += WebClientOnonData;
        
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
                        Destroy(newPlayer.GetComponent<PlayerController>());
                        PositionInterp positionInterp = newPlayer.GetComponent<PositionInterp>();
                        positionInterp.enabled = true;
                        _otherPlayers[id] = positionInterp;
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
                switch (_currentState)
                {
                    case GameState.Waiting:
                    {
                        Debug.Log("New state: Waiting");
                        break;
                    }
                    case GameState.Begin:
                    {
                        Debug.Log("New state: Begin");
                        break;
                    }
                    case GameState.Builder:
                    {
                        Debug.Log("New state: Builder");
                        break;
                    }
                    case GameState.Search:
                    {
                        Debug.Log("New state: Search");
                        break;
                    }
                }
                
                break;
            }
        }
    }

    private void WebClientOnonConnect()
    {
        Debug.Log("Client connected");
    }

    private void LateUpdate()
    {
        _webClient.ProcessMessageQueue(this);

        if (Time.time >= _timeToSendNextUpdate)
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
}
