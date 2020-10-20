using System.Threading;
using System;
using System.Net;
using System.Net.Sockets;

namespace FTPserver{
    class Server{
        private TcpListener listener;

        public Server(){}

        public void Start(IPAddress address){
            listener = new TcpListener(address, 21);
            listener.Start();
            listener.BeginAcceptTcpClient(ClientHandler, listener);
            Log.StartLog();
        }

        public void Start(){
            Start(IPAddress.Any);
        }

        public void Stop(){
            if(listener != null){
                listener.Stop();

            }
            Log.StopLog();
        }

        private void ClientHandler(IAsyncResult result){
            listener.BeginAcceptTcpClient(ClientHandler, listener);
            TcpClient client = listener.EndAcceptTcpClient(result);

            ClientHandle clientHandle = new ClientHandle(client);

            ThreadPool.QueueUserWorkItem(clientHandle.ClientHandler, client);
        }
    }
}