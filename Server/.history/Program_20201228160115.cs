using System;
using Mirror.SimpleWeb;
using System.Security.Authentication;
using System.Collections.Generic;

namespace Server
{
    struct PlayerData {
        int data;
        byte[] position;
        ushort points;
    }
    class Program
    {
        private static SimpleWebServer _webServer;
        private static Dictionary<int, PlayerData> _playerDict;
        private static Queue<

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

            _webServer.onConnect += WebServerOnConnect;
            
            _webServer.onData += WebServerOnData;

            while (!Console.KeyAvailable) {
                _webServer.ProcessMessageQueue();
            }

            Console.WriteLine("Closing server");
            _webServer.Stop();
        }

        static void WebServerOnConnect(int id) {
            _playerDict[id] = 
        }

        static void WebServerOnData(int id, ArraySegment<byte> data) {

        }
    }
}
