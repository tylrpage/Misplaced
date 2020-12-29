using System;
using Mirror.SimpleWeb;
using System.Security.Authentication;
using System.Collections.Generic;
using NetStack.Serialization;
using NetStack.Quantization;
using System.Timers;
using System.Linq;

namespace Server
{
    public class PlayerData {
        public ushort id;
        public bool isNew;
        public uint qX;
        public uint qY;
        public ushort points;
        public List<ushort> guesses;

        // Default ctor
        public PlayerData() {
            id = ushort.MaxValue;
            isNew = true;
            qX = uint.MaxValue;
            qY = uint.MaxValue;
            points = 0;
            guesses = new List<ushort>();
        }

        // Copy ctor
        public PlayerData(PlayerData copy) {
            id = copy.id;
            qX = copy.qX;
            qY = copy.qY;
            points = copy.points;
            guesses = copy.guesses;
        }
    }
    class Program
    {
        public static readonly float SECONDS_WAITING_IN_BEGIN = 3f;
        public static readonly float SECONDS_WAITING_IN_BUILD = 15f;
        public static readonly float SECONDS_WAITING_IN_SEARCH = 30f;

        private static SimpleWebServer _webServer;
        private static List<int> _connectedIds = new List<int>();
        private static Dictionary<int, PlayerData> _playerDatas = new Dictionary<int, PlayerData>();
        private static Queue<PlayerData> _dataToSend = new Queue<PlayerData>();

        private static BitBuffer _bitBuffer = new BitBuffer(1024);
        private static byte[] _buffer = new byte[2048];

        enum GameState {Waiting = 0, Begin, Builder, Search, Scoring}
        private static GameState _currentState = GameState.Waiting;
        private static List<ushort> _movedObjects;
        private static bool _waitingOnStateTimer = false;
        private static float _builderId;

        private static Random _rand;

        static void Main(string[] args)
        {
            _rand = new Random(Environment.TickCount);

            SslConfig sslConfig;
            TcpConfig tcpConfig = new TcpConfig(true, 5000, 20000);
            Console.WriteLine("Setting up secure server");
            sslConfig = new SslConfig(true, "cert.pfx", "", SslProtocols.Tls12);
            _webServer = new SimpleWebServer(10000, tcpConfig, 16*1024, 3000, sslConfig);
            _webServer.Start(Constants.GAME_PORT);
            Console.WriteLine("Server started");

            _webServer.onConnect += WebServerOnConnect;
            _webServer.onData += WebServerOnData;
            _webServer.onDisconnect += WebServerOnDisconnect;

            Timer stateUpdateTimer = new Timer(1f / Constants.SERVER_TICKRATE * 1000);
            stateUpdateTimer.Elapsed += StateUpdateTimerOnElapsed;
            stateUpdateTimer.AutoReset = true;
            stateUpdateTimer.Enabled = true;

            while (!Console.KeyAvailable) {
                _webServer.ProcessMessageQueue();

                // GUARD, DONT DO STATE STUFF IF WE ARE WAITING
                if (_waitingOnStateTimer) continue;
                switch(_currentState) {
                    case GameState.Waiting:
                    {
                        if (_connectedIds.Count >= 2) {
                            _currentState = GameState.Begin;
                            SendStateUpdate(_currentState);
                        }

                        break;
                    }
                    case GameState.Begin:
                    {
                        // Set timer to go to builder state
                        Timer beginTimer = new Timer(SECONDS_WAITING_IN_BEGIN * 1000);
                        beginTimer.AutoReset = false;
                        beginTimer.Start();
                        _waitingOnStateTimer = true;

                        beginTimer.Elapsed += delegate(Object source, ElapsedEventArgs e) {
                            _waitingOnStateTimer = false;

                            _movedObjects = new List<ushort>();
                            _currentState = GameState.Builder;
                            SendStateUpdate(_currentState);
                        };
                        break;
                    }
                    case GameState.Builder:
                    {
                        // Set timer to go to builder state
                        Timer buildTimer = new Timer(SECONDS_WAITING_IN_BUILD * 1000);
                        buildTimer.AutoReset = false;
                        buildTimer.Start();
                        _waitingOnStateTimer = true;

                        buildTimer.Elapsed += delegate(Object source, ElapsedEventArgs e) {
                            _waitingOnStateTimer = false;

                            // Reset everyones guesses
                            foreach (PlayerData playerData in _playerDatas.Values) {
                                playerData.guesses.Clear();
                            }
                            _currentState = GameState.Search;
                            SendStateUpdate(_currentState);
                        };
                        break;
                    }
                    case GameState.Search:
                    {
                        // Set timer to go to scoring state
                        Timer searchTimer = new Timer(SECONDS_WAITING_IN_SEARCH * 1000);
                        searchTimer.AutoReset = false;
                        searchTimer.Start();
                        _waitingOnStateTimer = true;

                        searchTimer.Elapsed += delegate(Object source, ElapsedEventArgs e) {
                            _waitingOnStateTimer = false;

                            _currentState = GameState.Scoring;
                            SendStateUpdate(_currentState);
                        };
                        break;
                    }
                    case GameState.Scoring:
                    {
                        foreach (var playerData in _playerDatas.Values) {
                            // GUARD, DON'T SCORE THE BUILDER THIS WAY
                            if (playerData.id == _builderId) continue;

                            int points;
                            _movedObjects.Intersect(playerData.guesses);
                        }
                    }
                }
            }

            Console.WriteLine("Closing server");
            _webServer.Stop();
        }

        static void WebServerOnConnect(int id) {
            _connectedIds.Add(id);
            _playerDatas[id] = new PlayerData() {
                id = (ushort)id
            };

            // Tell new client their id and the game state
            _bitBuffer.Clear();
            _bitBuffer.AddByte(2);
            _bitBuffer.AddUShort((ushort)id);
            _bitBuffer.AddByte((byte)_currentState);
            _bitBuffer.ToArray(_buffer);
            _webServer.SendOne(id, new ArraySegment<byte>(_buffer, 0, 3));
        }

        static void WebServerOnData(int id, ArraySegment<byte> data) {
            _bitBuffer.Clear();
            _bitBuffer.FromArray(data.Array, data.Count);

            byte messageId = _bitBuffer.ReadByte();
            switch (messageId) {
                case 1:
                {
                    uint qX = _bitBuffer.ReadUInt();
                    uint qY = _bitBuffer.ReadUInt();

                    PlayerData playerData = _playerDatas[id];
                    playerData.qX = qX;
                    playerData.qY = qY;

                    // Send this position to everyone next state packet
                    _dataToSend.Enqueue(playerData);
                    
                    break;
                }
            }
        }

        static void WebServerOnDisconnect(int id) {
            _connectedIds.Remove(id);
            _playerDatas.Remove(id);

            // Tell other players about the disconnection
            _bitBuffer.Clear();
            _bitBuffer.AddByte(4);
            _bitBuffer.AddUShort((ushort)id);
            _bitBuffer.ToArray(_buffer);
            _webServer.SendAll(_connectedIds, new ArraySegment<byte>(_buffer, 0, 3));
        }

        private static void StateUpdateTimerOnElapsed(Object source, ElapsedEventArgs e) {
            _bitBuffer.Clear();
            _bitBuffer.AddByte(3);
            _bitBuffer.AddUShort((ushort)_dataToSend.Count);
            foreach (PlayerData playerData in _dataToSend) {
                _bitBuffer.AddUShort(playerData.id);
                _bitBuffer.AddUInt(playerData.qX);
                _bitBuffer.AddUInt(playerData.qY);
            }

            _bitBuffer.ToArray(_buffer);
            _webServer.SendAll(_connectedIds, new ArraySegment<byte>(_buffer, 0, 3 + 10 * _dataToSend.Count));

            _dataToSend.Clear();
        }

        private static void SendStateUpdate(GameState currentState) {
            Console.WriteLine("Changing state to: " + currentState.ToString());

            _bitBuffer.Clear();
            _bitBuffer.AddByte(5);
            _bitBuffer.AddByte((byte)currentState);

            // Chose a random builder and tell everyone
            if (currentState == GameState.Begin) {
                    int randomIndex = _rand.Next(0, _connectedIds.Count);
                    _builderId = _connectedIds[randomIndex];
                    _bitBuffer.AddUShort((ushort)_builderId);
            }

            _bitBuffer.ToArray(_buffer);
            _webServer.SendAll(_connectedIds, new ArraySegment<byte>(_buffer, 0, 4));
        }
    }
}
