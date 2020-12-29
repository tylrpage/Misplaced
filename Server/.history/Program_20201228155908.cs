using System;
using Mirror.SimpleWeb;
using System.Security.Authentication;
using System.Collections.Generic;

namespace Server
{
    struct PlayerData {
        byte[] position;
        ushort points;
    }
    class Program
    {
        private static SimpleWebServer _webServer;
        private static Dictionary<int, PlayerData> _playerDict;

        enum GameState {Waiting, Begin, Builder, Search, Scoring}
        static GameState _currentState = GameState.Waiting;

        static void Main(string[] args)
        {
            SslConfig sslConfig;
            TcpConfig tcpConfig = new TcpConfig(true, 5000, 20000);
            Console.WriteLine("Setting up secure server");
            sslConfig = new SslConfig(true, "cert.pfx", "", SslProtocols.Tls12);
            _webServer = new SimpleWebServer(10000, tcpConfig, 16*1024, 3000, sslConfig);
            _webServer.Start(Constants.GAME_PORT);
            Console.WriteLine("Server started");

            _webServer.onConnect += delegate(int i) {
                Console.WriteLine($"Client {i} joined, sending hello");
                byte[] buff = System.Text.Encoding.UTF8.GetBytes("Hello from server");
                _webServer.SendOne(i, new ArraySegment<byte>(buff));
            };
            
            _webServer.onData += delegate(int i, ArraySegment<byte> data) {
                string msg = System.Text.Encoding.UTF8.GetString(data.Array);
                Console.WriteLine($"Client {i} send: {msg}");
            };

            while (!Console.KeyAvailable) {
                _webServer.ProcessMessageQueue();
            }

            Console.WriteLine("Closing server");
            _webServer.Stop();
        }
    }
}
