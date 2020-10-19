using System;

namespace FTP_server
{
    class Program
    {
        static void Main(string[] args)
        {
            FTPServer server = new FTPServer();
            server.Start();
            Console.ReadKey();
            server.Stop();
        }
    }
}
