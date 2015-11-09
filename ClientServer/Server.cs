using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace ClientServer
{
    public class Server
    {
        private int mNextClientID = 0;
        private bool mListening = false;
        private Authenticator mAuth;
        private Socket mListener;
        private Dictionary<int, Client> mConnectedClients;
        private Thread mListenerThread;
        private Thread mPollingThread;
        private ManualResetEvent mPollStart;
        private int mMaxClients = 1;
        private ManualResetEvent listenComplete;

        public event Events.ListenerStartedDelegate ListenerStarted;
        public event Events.ServerErrorDelegate ServerError;
        public event Events.ListenerStoppedDelegate ListenerStopped;

        public event Events.ClientConnectedDelegate ClientConnected;
        public event Events.ClientDisconnectedDelegate ClientDisconnected;
        public event Events.ClientErrorDelegate ClientError;
        public event Events.ClientPacketReceivedDelegate ClientPacketReceived;
        public event Events.ClientFileReceivedDelegate ClientFileReceived;


        public Server()
        {
            listenComplete = new ManualResetEvent(false);
            mAuth = new Authenticator();
            mConnectedClients = new Dictionary<int, Client>();
            mListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        
        public void Start(int port)
        {
            mPollStart = new ManualResetEvent(false);
            mPollStart.Reset();
            mListenerThread = new Thread(new ParameterizedThreadStart(Listen));
            mListenerThread.Name = String.Format("Server Listener Thread on port {0}", port);
            mListenerThread.Start(port);
            mPollStart.WaitOne();

            mPollingThread = new Thread(new ThreadStart(PollClients));
            mPollingThread.Name = String.Format("Server Polling Thread");
            mPollingThread.Start();
        }
        public void Stop()
        {
            mListening = false;
            try
            {
                mListener.Shutdown(SocketShutdown.Both);
            }
            catch(Exception ex)
            {
                mListenerThread.Abort();
                mPollingThread.Abort();
            }
            finally
            {
                if (ListenerStopped != null) ListenerStopped();
            }
        }
        private void PollClients()
        {
            try
            {
                while (mListening)
                {
                    Thread.Sleep(200);
                    lock (mConnectedClients)
                    {
                        List<int> keys = new List<int>();
                        keys.AddRange(mConnectedClients.Keys);

                        for (int k = 0; k < keys.Count; k++)
                        {
                            if (!mConnectedClients[keys[k]].Connected)
                            {
                                RemoteClient_ClientDisconnected(mConnectedClients[keys[k]].ID);
                                mConnectedClients.Remove(keys[k]);
                            }
                        }
                    }
                }
            }
            catch(ThreadAbortException TAex)
            {

            }
        }
        public void Shutdown()
        {
            Stop();
            lock (mConnectedClients)
            {
                List<int> keys = new List<int>();
                keys.AddRange(mConnectedClients.Keys);

                for (int k = 0; k < keys.Count; k++)
                    mConnectedClients[keys[k]].Close();
            }
        }

        public int MaxClients { get { return mMaxClients; } set { mMaxClients = value; } }
        public int Clients { get { lock (mConnectedClients) { return mConnectedClients.Count; } } }
        public bool Listening { get { return mListening; } }

        private void Listen(object port)
        {
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, (int)port);
                mListener.Bind(ep);
                mListener.Listen(5);
                mListening = true;
                if (ListenerStarted != null) ListenerStarted();
                mPollStart.Set();

                while (mListening)
                {
                    if (this.Clients >= this.MaxClients)
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        //Socket acceptedSocket = mListener.Accept();
                        listenComplete.Reset();
                        mListener.BeginAccept(new AsyncCallback(ListeningSocketAsyncCallback), mListener);
                        listenComplete.WaitOne();
                    }
                }
            }
            catch (ThreadAbortException taEx) { mListening = false; }
            catch (Exception ex) { if (ServerError != null) ServerError(ex.Message); }
            finally { }
        }
        private void ListeningSocketAsyncCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket newSocket = listener.EndAccept(ar);

                // Additional code to read data goes here. 
                lock (mConnectedClients)
                {
                    Client RemoteClient = new Client(mNextClientID, newSocket, true, mAuth);
                    if ((RemoteClient.Connected) && (RemoteClient.Authenticated))
                    {
                        RemoteClient.OnError += new Events.ClientErrorDelegate(RemoteClient_ClientError);
                        RemoteClient.OnPacketReceived += new Events.ClientPacketReceivedDelegate(RemoteClient_ClientPacketReceived);
                        RemoteClient.OnFileReceived += new ClientServer.Events.ClientFileReceivedDelegate(RemoteClient_OnFileReceived);
                        mConnectedClients.Add(RemoteClient.ID, RemoteClient);
                        if (ClientConnected != null) ClientConnected(mNextClientID);
                        mNextClientID++;
                    }
                }
            }
            catch(Exception ex)
            {

            }
            finally
            {
                listenComplete.Set();
            }
        }
        void RemoteClient_OnFileReceived(int ConnectionID, FileInfo fiTempFile)
        {
            if (ClientFileReceived != null) ClientFileReceived(ConnectionID, fiTempFile);
        }
        void RemoteClient_ClientDisconnected(int ConnectionID)
        {
            if (ClientDisconnected != null) ClientDisconnected(ConnectionID);
            lock (mConnectedClients)
            {
                if (mConnectedClients.ContainsKey(ConnectionID)) mConnectedClients.Remove(ConnectionID);
            }
        }
        void RemoteClient_ClientError(int ConnectionID, string ErrorMessage)
        {
            if (ClientError != null) ClientError(ConnectionID, ErrorMessage);
        }
        void RemoteClient_ClientPacketReceived(int ConnectionID, byte[] data)
        {
            if (ClientPacketReceived != null) ClientPacketReceived(ConnectionID, data);
        }

        public void Broadcast(string Text)
        {
            lock (mConnectedClients)
            {
                List<int> connIDs = new List<int>();
                connIDs.AddRange(mConnectedClients.Keys);
                foreach (int connID in connIDs)
                {
                    SendTo(connID, Text);
                }
            }
        }
        public void Broadcast(byte[] data)
        {
            lock (mConnectedClients)
            {
                List<int> connIDs = new List<int>();
                connIDs.AddRange(mConnectedClients.Keys);
                foreach (int connID in connIDs)
                {
                    SendTo(connID, data);
                }
            }
        }
        
        public void SendTo(int ConnectionID, string Text)
        {
            lock (mConnectedClients)
            {
                if (!mConnectedClients.ContainsKey(ConnectionID)) return;

                Client c = mConnectedClients[ConnectionID];
                if (c.Connected)
                    c.Send(Text);
                else
                    mConnectedClients.Remove(ConnectionID);
            }
        }
        public void SendTo(int ConnectionID, byte[] data)
        {
            lock (mConnectedClients)
            {
                if (!mConnectedClients.ContainsKey(ConnectionID)) return;

                Client c = mConnectedClients[ConnectionID];
                if (c.Connected)
                    c.Send(data);
                else
                    mConnectedClients.Remove(ConnectionID);
            }
        }
        public void SendTo(int ConnectionID, FileInfo FileToSend)
        {
            lock (mConnectedClients)
            {
                if (!mConnectedClients.ContainsKey(ConnectionID)) return;

                Client c = mConnectedClients[ConnectionID];
                if (c.Connected)
                    c.Send(FileToSend);
                else
                    mConnectedClients.Remove(ConnectionID);
            }
        }
        public void AddUser(string UserName, string Password)
        {
            if (mAuth != null) mAuth.AddUser(UserName, Password);
        }
        public void RemoveUser(string UserName)
        {
            if (mAuth != null) mAuth.RemoveUser(UserName);
        }

    }
}
