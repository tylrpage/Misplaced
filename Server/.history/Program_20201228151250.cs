using System;
using Mirror.SimpleWeb;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Server
{
    class Program
    {
        private static SimpleWebServer _webServer;

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

            }
        }
    }
}
