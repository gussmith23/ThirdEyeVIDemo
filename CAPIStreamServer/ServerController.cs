using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CAPIStreamCommon;
namespace CAPIStreamServer
{
    public enum ConnectionType
    {
        UDP = 0,
        TCP = 1
    };
    public class ServerController
    {
        Dictionary<PacketType, ImageWork> delegateFunctions = new Dictionary<PacketType, ImageWork>();
        Socket listener;
        private int clientCount = 0;
        private int clientId = 0;
        private Boolean runServer;
        private Thread serverAcceptThread;
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private List<ServerWork> openConnections = new List<ServerWork>();
        public ServerController()
        {
            clientCount = 0;
            clientId = 0;
        }
        public int startServer(ConnectionType type)
        {
            runServer = true;
            if (type == ConnectionType.TCP)
            {
                //TODO: Open TCP Connection
                Console.WriteLine("Attempting to open TCP connection");
                openTCPConnection();
            }
            else
            {
                //TODO: Open UDP Connection
            }
            return 0;
        }
        public int shutdownServer()
        {
            if(serverAcceptThread != null)
            {
                runServer = false;
                serverAcceptThread.Join();
            }
            listener.Close();
            foreach(ServerWork x in openConnections)
            {
                if (x.socket.Connected == true)
                {
                    x.stopWork();
                }
            }
            return 0;
        }
        private int openTCPConnection()
        {
            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            // Create a TCP/IP socket. listen for connection from any address
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 2275);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                Console.WriteLine("Connection Openned");
                serverAcceptThread = new Thread(new ThreadStart(runServerAccept));
                serverAcceptThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return 0;
        }
        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            // Create the state object.
            ServerWork newClient = new ServerWork();
            newClient.clientID = (uint)++clientId;
            ++clientCount;
            newClient.registerFunctions(delegateFunctions);
            newClient.startWork(handler);
            openConnections.Add(newClient);
        }
        private void runServerAccept()
        {
            Console.WriteLine("Server Accept Thread Started");
            while (runServer == true)
            {
                // Set the event to nonsignaled state.
                allDone.Reset();
                // Start an asynchronous socket to listen for connections.
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                // Wait until a connection is made before continuing.
                allDone.WaitOne();
            }
        }
        public void registerDelegate(PacketType type, ImageWork func)
        {
            if (delegateFunctions.ContainsKey(type))
            {
                delegateFunctions[type] = func;
            }
            else
            {
                delegateFunctions.Add(type, func);
            }
        }
    }

}
