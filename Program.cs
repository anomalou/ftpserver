using System.Net;
using System;

namespace FTPserver
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Start(new IPAddress(new byte[4]{192,168,1,95}));
            Console.ReadKey();
            server.Stop();
        }
    }
}
