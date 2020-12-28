using System;
using WebSocketSharp.NetCore;
using WebSocketSharp.NetCore.Server;

namespace Server
{
    class Program
    {
        public class Game : WebSocketBehavior {
            protected override void OnOpen()
            {
                Console.WriteLine("Client connected");
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
