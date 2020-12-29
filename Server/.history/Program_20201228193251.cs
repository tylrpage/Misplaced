using System;
using Mirror.SimpleWeb;
using System.Security.Authentication;
using System.Collections.Generic;
using NetStack.Serialization;
using NetStack.Quantization;
using System.Timers;

namespace Server
{
    public class PlayerData {
        public ushort id;
        public bool isNew;
        public uint qX;
        public uint qY;
        public ushort points;

        // Default ctor
        public PlayerData() {
            id = ushort.MaxValue;
            isNew = true;
            qX = uint.MaxValue;
            qY = uint.MaxValue;
            points = 0;
        }

        // Copy ctor
        public PlayerData(PlayerData copy) {
            id = copy.id;
            qX = copy.qX;
            qY = copy.qY;
            points = copy.points;
        }
    }
    class Program
    {
        private static SimpleWebServer _webServer;
        private static List<int> _connectedIds = new List<int>();
        private static Dictionary<int, PlayerData> _playerDatas = new Dictionary<int, PlayerData>();
        private static Queue<PlayerData> _dataToSend = new Queue<PlayerData>();

        private static BitBuffer _bitBuffer = new BitBuffer(1024);
        private static byte[] _buffer = new byte[2048];

        enum GameState {Waiting = 0, Begin, Builder, Search, Scoring}
        private static GameState _currentState = GameState.Waiting;

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
                        


                        break;
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

            // Tell new client their id
            _bitBuffer.Clear();
            _bitBuffer.AddByte(2);
            _bitBuffer.AddUShort((ushort)id);
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
            _bitBuffer.Clear();
            _bitBuffer.AddByte(5);
            _bitBuffer.AddByte((byte)currentState);

            switch(currentState) {
                case GameState.Begin: {
                    int randomIndex = _rand.Next(0, _connectedIds.Count);
                    int nextBuilderId = _connectedIds[randomIndex];
                    _bitBuffer.AddUShort((ushort)nextBuilderId);

                    break;
                }
            }

            _bitBuffer.ToArray(_buffer);
            _webServer.SendAll(_connectedIds, new ArraySegment<byte>(_buffer, 0, 2));
        }
    }
}
