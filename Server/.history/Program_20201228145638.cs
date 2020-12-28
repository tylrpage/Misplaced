using System;
using Mirror.SimpleWeb;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Server
{
    class Program
    {
        private SimpleWebServer _webServer;

        static void Main(string[] args)
        {
            SslConfig sslConfig;
            TcpConfig tcpConfig = new TcpConfig(true, 5000, 20000);
            Console.WriteLine("Setting up secure server");
            sslConfig = new SslConfig(true, "cert.pfx", "", SslProtocols.Tls12);
        }
    }
}
