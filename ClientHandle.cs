using System.Text;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace FTP_server{

    enum DataConnectionType{
        Active,
        Passive
    }
    class ClientHandle{
        private TcpClient controlClient;
        private NetworkStream controlStream;
        private StreamWriter controlWriter;
        private StreamReader controlReader;

        private TcpListener passiveListener;

        private string username;
        private string typeCode;
        private string currentDirectory;

        private DataConnectionType dataConnectionType;
        private IPEndPoint dataEndPoint;
        private TcpClient dataClient;

        private StreamWriter dataWriter;
        private StreamReader dataReader;

        private string root;

        public ClientHandle(TcpClient client){
            controlClient = client;

            controlStream = controlClient.GetStream();

            controlWriter = new StreamWriter(controlStream);
            controlReader = new StreamReader(controlStream);

            root = "C:\\";
        }

        public void ClientHandler(object obj){
            controlWriter.WriteLine("220 Service ready!");
            controlWriter.Flush();

            string line = null;

            try{
                while(!string.IsNullOrEmpty(line = controlReader.ReadLine())){
                    string response = null;

                    string[] command = line.Split(' ');
                    string cmd = command[0].ToUpperInvariant();
                    string args = command.Length > 1 ? line.Substring(command[0].Length + 1) : null;

                    if(string.IsNullOrWhiteSpace(args))
                        args = null;

                    if(response == null){
                        switch(cmd){
                            case "USER":
                                response = User(args);
                                break;
                            case "PASS":
                                response = Password(args);
                                break;
                            case "CWD":
                                response = ChangeWorkingDirectory(args);
                                break;
                            case "CDUP":
                                response = ChangeWorkingDirectory("..");
                                break;
                            case "PWD":
                                response = "257 \"/\" is current directory.";
                                break;
                            case "TYPE":
                                string[] typeArgs = args.Split(' ');
                                response = Type(typeArgs[0], typeArgs.Length > 1 ? typeArgs[1] : null);
                                break;
                            case "PASV":
                                response = Pasv();
                                break;
                            case "PORT":
                                response = Port(args);
                                break;
                            case "LIST":
                                response = List(args);
                                break;
                            case "RETR":
                                response = Retr(args);
                                break;
                            case "QUIT":
                                response = "221 Service closing control connection";
                                break;

                            default:
                                response = "502 Command not implemented";
                                break;
                        }
                    }

                    if(controlClient == null || !controlClient.Connected){
                        break;
                    }else{
                        controlWriter.WriteLine(response);
                        controlWriter.Flush();

                        if(response.StartsWith("221"))
                            break;
                    }
                }
            }catch(Exception ex){
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private string User(string args){
            username = args;
            return "331 Username ok, need password";
        }

        private string Password(string args){
            if (true)
            {
                return "230 User logged in";
            }
            else
            {
                return "530 Not logged in";
            }
        }

        private string Type(string typeCode, string format){
            string response = null;

            switch(typeCode){
                case "A":
                    response = "220 OK";
                    break;
                case "I":
                    this.typeCode = typeCode;
                    response = "220 OK";
                    break;
                case "E":
                case "L":
                default:
                    response = "504 Command not implemented for that parameter.";
                    break; 
            }

            if (format != null)
            {
                switch (format)
                {
                    case "N":
                        response = "200 OK";
                        break;
                    case "T":
                    case "C":
                    default:
                        response = "504 Command not implemented for that parameter.";
                        break;
                }
            }

            return response;
        }

        private string Port(string args){
            string[] address = args.Split(' ');

            byte[] addressArray = new byte[4];
            addressArray[0] = byte.Parse(address[0]);
            addressArray[1] = byte.Parse(address[1]);
            addressArray[2] = byte.Parse(address[2]);
            addressArray[3] = byte.Parse(address[3]);

            byte[] portArray = new byte[2];
            portArray[0] = byte.Parse(address[4]);
            portArray[1] = byte.Parse(address[5]);

            IPAddress pAddress = new IPAddress(addressArray);

            if(BitConverter.IsLittleEndian)
                Array.Reverse(portArray);

            short port = BitConverter.ToInt16(portArray, 0);

            dataEndPoint = new IPEndPoint(pAddress, port);

            return $"227 Entered active mode";
        }

        private string Pasv(){
            IPAddress localAddress = ((IPEndPoint)controlClient.Client.LocalEndPoint).Address;

            passiveListener = new TcpListener(localAddress, 0);
            passiveListener.Start();

            IPEndPoint localEndPoint = ((IPEndPoint)passiveListener.LocalEndpoint);

            byte[] address = localEndPoint.Address.GetAddressBytes();
            short port = (short)localEndPoint.Port;

            byte[] portArray = BitConverter.GetBytes(port);

            if(BitConverter.IsLittleEndian)
                Array.Reverse(portArray);

            dataConnectionType = DataConnectionType.Passive;
            
            return $"227 Entering passive mode ({address[0]},{address[1]},{address[2]},{address[3]},{portArray[0]},{portArray[1]})";
        }

        private string List(string pathname){
            if(pathname == null){
                pathname = string.Empty;
            }

            pathname = new DirectoryInfo(Path.Combine(currentDirectory, pathname)).FullName;
            
            if(IsPathValid(pathname)){
                if(dataConnectionType == DataConnectionType.Active){
                    dataClient = new TcpClient();
                    dataClient.BeginConnect(dataEndPoint.Address, dataEndPoint.Port, DoList, pathname);
                }else{
                    passiveListener.BeginAcceptTcpClient(DoList, pathname);
                }

                return $"150 Opened {dataConnectionType} mode data transfer for LIST";
            }

            return "450 Request path do not exites";
        }

        private string ChangeWorkingDirectory(string args){
            currentDirectory = args;
            return "250 Changed to new directory";
        }

        private string Retr(string pathname){
            pathname = NormalizeFilename(pathname);
            
            if(IsPathValid(pathname)){
                if(File.Exists(pathname)){
                    if(dataConnectionType == DataConnectionType.Active){
                        dataClient = new TcpClient();
                        dataClient.BeginConnect(dataEndPoint.Address, dataEndPoint.Port, DoRetrieve, pathname);
                    }else{
                        passiveListener.BeginAcceptTcpClient(DoRetrieve, pathname);
                    }

                    return $"150 Opened in {dataConnectionType} transfer mode for RETR";
                }
            }

            return "550 File not found";
        }

        private string NormalizeFilename(string name){
            if(name == null){
                name = string.Empty;
            }

            if(name == "/"){
                return root;
            }else if(name.StartsWith('/')){
                name = new FileInfo(Path.Combine(root, name.Substring(1))).FullName;
            }else{
                string[] clearName = name.Split(' ');
                name = new FileInfo(Path.Combine(currentDirectory, clearName[clearName.Length - 1])).FullName;
            }
            
            return IsPathValid(name) ? name : null;
        }

        private bool IsPathValid(string path)
        {
            return path.StartsWith(root);
        }

        private void DoRetrieve(IAsyncResult result){
            if(dataConnectionType == DataConnectionType.Active){
                dataClient.EndConnect(result);
            }else{
                dataClient = passiveListener.EndAcceptTcpClient(result);
            }

            string pathname = (string)result.AsyncState;

            using(NetworkStream dataStream = dataClient.GetStream()){
                using(FileStream fs = new FileStream(pathname, FileMode.Open, FileAccess.Read)){
                    CopyStream(fs, dataStream);

                    dataClient.Close();
                    dataClient = null;

                    controlWriter.WriteLine("226 Closing data connection");
                    controlWriter.Flush();
                }
            }
        }

        private static long CopyStream(Stream input, Stream output, int bufferSize){
            byte[] buffer = new byte[bufferSize];
            int count = 0;
            long total = 0;
            
            while((count = input.Read(buffer, 0, buffer.Length)) > 0){
                output.Write(buffer, 0, count);
                total += count;
            }

            return total;
        }

        private static long CopyStreamAscii(Stream input, Stream output, int bufferSize){
            char[] buffer = new char[bufferSize];
            int count = 0;
            long total = 0;

            using(StreamReader rdr = new StreamReader(input)){
                using(StreamWriter wtr = new StreamWriter(output, Encoding.ASCII)){
                    while((count = rdr.Read(buffer, 0, buffer.Length)) > 0){
                        wtr.Write(buffer, 0, count);
                        total += count;
                    }
                }
            }

            return total;
        }

        private long CopyStream(Stream input, Stream output){
            if(typeCode == "I"){
                return CopyStream(input, output, 4096);
            }else{
                return CopyStreamAscii(input, output, 4096);
            }
        }

        private void DoList(IAsyncResult result){
            if(dataConnectionType == DataConnectionType.Active){
                dataClient.EndConnect(result);
            }else{
                dataClient = passiveListener.EndAcceptTcpClient(result);
            }

            string pathname = (string)result.AsyncState;

            using(NetworkStream dataStream = dataClient.GetStream()){
                dataWriter = new StreamWriter(dataStream, Encoding.ASCII);
                dataReader = new StreamReader(dataStream, Encoding.ASCII);

                IEnumerable<string> directories = Directory.EnumerateDirectories(pathname);

                foreach(string dir in directories){
                    DirectoryInfo d = new DirectoryInfo(dir);

                    string date = d.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180) ? d.LastWriteTime.ToString("MMM dd  yyyy") : d.LastWriteTime.ToString("MMM dd HH:mm");
                    string line = string.Format("drwxr-xr-x    2 2003     2003     {0,8} {1} {2}", "4096", date, d.Name);

                    dataWriter.WriteLine(line);
                    dataWriter.Flush();
                }

                IEnumerable<string> files = Directory.EnumerateFiles(pathname);

                foreach(string file in files){
                    FileInfo f = new FileInfo(file);

                    string date = f.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180) ? f.LastWriteTime.ToString("MMM dd  yyyy") : f.LastWriteTime.ToString("MMM dd HH:mm");
                    string line = string.Format("-rw-r--r--    2 2003     2003     {0,8} {1} {2}", f.Length, date, f.Name);

                    dataWriter.WriteLine(line);
                    dataWriter.Flush();
                }

                dataClient.Close();
                dataClient = null;

                controlWriter.WriteLine("226 Transfer complete");
                controlWriter.Flush();
            }
        }
    }
}