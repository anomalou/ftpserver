using System.Threading;
using System.Text;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FTP_server{
    class FTPServer{
        private TcpListener listener;

        public FTPServer(){}

        public void Start(){
            listener = new TcpListener(IPAddress.Any, 21);
            listener.Start();
            listener.BeginAcceptTcpClient(ClientHandler, listener);
        }

        public void Stop(){
            if(listener != null){
                listener.Stop();
            }
        }

        private void ClientHandler(IAsyncResult result){
            listener.BeginAcceptTcpClient(ClientHandler, listener);
            TcpClient client = listener.EndAcceptTcpClient(result);

            ClientHandle clientHandle = new ClientHandle(client);

            ThreadPool.QueueUserWorkItem(clientHandle.ClientHandler, client);
        }
    }
}