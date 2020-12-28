using System;
using Mirror.SimpleWeb;

namespace Server
{
    class Program
    {
        private SimpleWebServer _webServer;

        static void Main(string[] args)
        {
            SslConfig sslConfig;
            TcpConfig tcpConfig = new TcpConfig(true, 5000, 20000);
        }
    }
}
