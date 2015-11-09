using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace ClientServer
{
    public class Client
    {
        private const int HEADER_SIZE = 24;
        private const int AUTH_RESPONSE_SIZE = 32;
        private const int AUTH_USER_OFFSET = 0;
        private const int AUTH_USER_SIZE = 16;
        private const int AUTH_PASS_OFFSET = 16;
        private const int AUTH_PASS_SIZE = 16;
        private const int FILE_NAME_SIZE = 256;


        private int mID;
        private bool mEncrypted;
        private bool mAuthenticated;
        private bool mServerSocket;

        private Socket mSocket;
        private CryptoManager mCrypto;
        private Thread mReceiverThread;

        public int ID { get { return mID; } }
        public bool Encrypted { get { return mEncrypted; } }
        public bool Authenticated { get { return mAuthenticated; } }
        public bool ServerSide { get { return mServerSocket; } }
        public bool Connected { get { CheckClientState(); return mSocket.Connected; } }

        public event Events.ClientConnectedDelegate OnConnected;
        public event Events.ClientPacketReceivedDelegate OnPacketReceived;
        public event Events.ClientFileReceivedDelegate OnFileReceived;
        public event Events.ClientDisconnectedDelegate OnDisconnected;
        public event Events.ClientErrorDelegate OnError;

        public Client()
        {
            Init();
        }
        internal Client(int ID, Socket sock, bool EnableEncryption, Authenticator auth)
        {
            mServerSocket = true;
            mID = ID;
            mSocket = sock;
            bool bOK = true;
            if (EnableEncryption) bOK = PerformKeyExchange();
            if ((auth != null) && (bOK)) bOK = ServerSideAuthentication(auth);
            if (auth == null) mAuthenticated = true;
            if (bOK)
                StartReceiver();
            else
                Close();
        }
        private void Init()
        {
            mServerSocket = false;
            mID = -1;
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mEncrypted = false;
            mAuthenticated = false;
        }

        #region Connect Overloads and Callback
        public void Connect(string Host, string Port)
        {
            if (this.ServerSide) return;
            int p = 0;
            if (int.TryParse(Port, out p))
            {
                Connect(Host, p);
            }
            else
            {
                if (OnError != null) OnError(this.ID, String.Format("Unable to parse connection information {0}:{1}.  Please verify the connection information is correct", Host, Port));
            }
        }
        public void Connect(string HostPort)
        {
            if (this.ServerSide) return;
            Connect(HostPort, null, null);
        }
        public void Connect(string Host, string Port, string User, string Password)
        {
            if (this.ServerSide) return;
            int p = 0;
            if (int.TryParse(Port, out p))
            {
                Connect(Host, p, User, Password);
            }
            else
            {
                if (OnError != null) OnError(this.ID, String.Format("Unable to parse connection information {2}@{0}:{1} using the given password.  Please verify the connection information is correct", Host, Port, User));
            }
        }
        public void Connect(string HostPort, string User, string Password)
        {
            if (HostPort.Contains(":"))
            {
                string[] split = HostPort.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                Connect(split[0], split[1], User, Password);
            }
            else
            {
                if (OnError != null) OnError(this.ID, String.Format("Unable to parse connection information {1}@{0} using the given password.  Please verify the connection information is correct", HostPort, User));
            }
        }

        public void Connect(string Host, int Port)
        {
            if (this.ServerSide) return;
            Connect(Host, Port, null, null);
        }
        public void Connect(string Host, int Port, string User, string Password)
        {
            if (this.ServerSide) return;
            Connect(Host, Port, User, Password, true);
        }
        private void Connect(string Host, int Port, string User, string Password, bool Retry)
        {
            if (this.ServerSide) return;
            try
            {
                if (mSocket.Connected) return;
                mSocket.BeginConnect(Host, Port, new AsyncCallback(CompleteConnect), new ConnectInfo(Host, Port, User, Password));
            }
            catch (ArgumentNullException ex) { if (OnError != null) OnError(this.ID, "Connect() failed: One of the arguments was invalid. Please verify your connection information and try again."); }
            catch (ArgumentOutOfRangeException ex) { if (OnError != null) OnError(this.ID, "Connect() failed: One of the arguments was invalid. Please verify your connection information and try again."); }
            catch (SocketException ex) { if (OnError != null) OnError(this.ID, "Connect() failed: The socket object was not ready. Please try again."); }
            catch (ObjectDisposedException ex)
            {
                Init(); if (Retry) { Connect(Host, Port, User, Password, false); } else { if (OnError != null) OnError(this.ID, "Connect() failed: The socket object was not ready. Please try again."); }
            }
            catch (NotSupportedException ex) { if (OnError != null) OnError(this.ID, "Connect() failed: The socket object was not ready. Please try again."); }
            catch (InvalidOperationException ex) { if (OnError != null) OnError(this.ID, "Connect() failed: The socket object was not ready. Please try again."); }
            catch (Exception ex) { if (OnError != null) OnError(this.ID, "Connect() failed: An unspecified error has occured. Please try again."); }
        }
        private void CompleteConnect(IAsyncResult ar)
        {
            try
            {
                mSocket.EndConnect(ar);
                ConnectInfo CI = ar.AsyncState as ConnectInfo;
                if (CI != null)
                {
                    if (mSocket.Connected)
                    {
                        // Socket connected
                        if (PerformKeyExchange())
                        {
                            // Key Exchange successful
                            // Authorization required
                            if (ClientSideAuthentication(CI.User, CI.Pass))
                            {
                                // Authorized
                                if (OnConnected != null) OnConnected(this.ID);
                                StartReceiver();
                            }
                            else
                            {
                                // Unauthorized
                                Close();
                            }
                        }
                        else
                        {
                            // Key Exchange failed
                            Close();
                        }
                    }
                    else
                    {
                        // Socket not connected
                        if (OnError != null) OnError(this.ID, (string)ar.AsyncState);
                        Close();
                    }
                }
                else
                {
                    // Do something here?
                    Close();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        public void Close()
        {
            if (this.Connected)
            {
                StopReceiver();
                mSocket.Close();
                CheckClientState();
                mEncrypted = false;
                mAuthenticated = false;
            }
        }

        private void CheckClientState()
        {
            bool bCurrentState = mSocket.Connected;
            bool blockingState = mSocket.Blocking;
            try
            {
                byte[] tmp = new byte[1];
                mSocket.Blocking = false;
                mSocket.Send(tmp, 0, 0);
            }
            catch { }
            finally { try { mSocket.Blocking = blockingState; } catch { } }
            // The socket WAS connected, but no longer is connected
            if (mSocket.Connected != bCurrentState) if (!mSocket.Connected) if (OnDisconnected != null) OnDisconnected(this.ID);
        }

        #region Key Exchange and Authentication Challenge
        private bool PerformKeyExchange()
        {
            PacketTypes t;
            PacketCodes c;
            int dLength;
            int eLength;
            int secret = new ThreadSafeRandom().Next(1, 2 * 65535);
            DiffieHellman.Init(6679881);
            long myA = DiffieHellman.GenerateExchange(secret);
            long otherA = 0;
            byte[] aBytes = BitConverter.GetBytes(myA);

            // Send the exchange
            byte[] outBuffer = new byte[HEADER_SIZE + aBytes.Length];
            WritePacketHeader(ref outBuffer, PacketTypes.Control, PacketCodes.KeyExchange, aBytes.Length, aBytes.Length);
            Buffer.BlockCopy(aBytes, 0, outBuffer, HEADER_SIZE, aBytes.Length);
            mSocket.Send(outBuffer);
            
            // Receive the response
            byte[] inBuffer = new byte[HEADER_SIZE + aBytes.Length];
            ReceiveHeader(ref inBuffer, out t, out c, out dLength, out eLength);

            if ((t == PacketTypes.Control) && (c == PacketCodes.KeyExchange) && (dLength > 0))
            {
                ReceiveData(ref inBuffer, dLength);
                otherA = BitConverter.ToInt64(inBuffer, 0);
                long Key = DiffieHellman.Extract(secret, otherA);
                mCrypto = new CryptoManager(Key);
            }

            // Send back the received value, encrypted
            byte[] otherABytes = BitConverter.GetBytes(otherA);
            byte[] eBytes = mCrypto.Encrypt(otherABytes);
            byte[] eBuffer = new byte[HEADER_SIZE + eBytes.Length];
            WritePacketHeader(ref eBuffer, PacketTypes.Control, PacketCodes.KeyExchange, otherABytes.Length, eBytes.Length);
            Buffer.BlockCopy(eBytes, 0, eBuffer, HEADER_SIZE, eBytes.Length);
            mSocket.Send(eBuffer);

            // Receive the response
            byte[] ineBuffer = new byte[HEADER_SIZE + eBytes.Length];
            ReceiveHeader(ref inBuffer, out t, out c, out dLength, out eLength);

            if ((t == PacketTypes.Control) && (c == PacketCodes.KeyExchange) && (dLength > 0))
            {
                ineBuffer = new byte[eLength];
                ReceiveData(ref ineBuffer, eLength);
                byte[] pData = mCrypto.Decrypt(ineBuffer);
                long ReceivedValue = BitConverter.ToInt64(pData, 0);
                mEncrypted = (ReceivedValue == myA);
            }
            return mEncrypted;
        }
        private bool ServerSideAuthentication(Authenticator auth)
        {
            PacketTypes t;
            PacketCodes c;
            int dLength;
            int eLength;
            int token = 0;
            byte[] tBytes = null;
            byte[] inBuffer = null;

            // (1) Send Challenge
            token = new ThreadSafeRandom().Next(1, 2 * 65535);
            tBytes = BitConverter.GetBytes(token);
            if (mEncrypted) tBytes = mCrypto.Encrypt(tBytes);
            System.Diagnostics.Debug.WriteLine(String.Format("Server Generated Challenge Auth Token: {0}", token));

            byte[] challengeBuffer = new byte[HEADER_SIZE + tBytes.Length];
            WritePacketHeader(ref challengeBuffer, PacketTypes.Control, PacketCodes.AuthChallenge, sizeof(int), tBytes.Length);
            Buffer.BlockCopy(tBytes, 0, challengeBuffer, HEADER_SIZE, tBytes.Length);
            mSocket.Send(challengeBuffer);
            
            // (3) Receive Challenge Response
            byte[] responseHeader = new byte[HEADER_SIZE];
            ReceiveHeader(ref responseHeader, out t, out c, out dLength, out eLength);
            if ((t == PacketTypes.Control) && (c == PacketCodes.AuthResponse) && (dLength > 0))
            {
                string uname = string.Empty;
                string pword = string.Empty;

                inBuffer = new byte[eLength];
                ReceiveData(ref inBuffer, eLength);
                if (mEncrypted)
                {
                    inBuffer = mCrypto.Decrypt(inBuffer);
                }

                // Extract token
                int receivedToken = BitConverter.ToInt32(inBuffer, 0);
                System.Diagnostics.Debug.WriteLine(String.Format("Server Received Response Auth Token: {0}", receivedToken));

                if (token == receivedToken)
                {
                    uname = UTF8Encoding.UTF8.GetString(inBuffer, 4, AUTH_USER_SIZE).Trim('\0');
                    pword = UTF8Encoding.UTF8.GetString(inBuffer, 4 + AUTH_USER_SIZE, AUTH_PASS_SIZE).Trim('\0');
                    mAuthenticated = auth.Authenticate(uname, pword);
                }
                else
                {
                    // Incorrect token returned
                    mAuthenticated = false;
                }
            }
            else
            {
                // Wrong packet parameters
                mAuthenticated = false;
            }

            // (5) Reply with Authorization Result
            if (mAuthenticated)
            {
                WritePacketHeader(ref challengeBuffer, PacketTypes.Control, PacketCodes.AuthOK, sizeof(int), tBytes.Length);
            }
            else
            {
                WritePacketHeader(ref challengeBuffer, PacketTypes.Control, PacketCodes.AuthReject, sizeof(int), tBytes.Length);
            }
            mSocket.Send(challengeBuffer);

            return mAuthenticated;
        }
        private bool ClientSideAuthentication(string Username, string Password)
        {
            Username = (String.IsNullOrEmpty(Username) ? string.Empty : Username);
            Password = (String.IsNullOrEmpty(Password) ? string.Empty : Password);

            PacketTypes t;
            PacketCodes c;
            int dLength;
            int eLength;
            int token = 0;
            byte[] inBuffer = null;

            // (2) Receive Challenge
            byte[] challengeHeader = new byte[HEADER_SIZE];
            ReceiveHeader(ref challengeHeader, out t, out c, out dLength, out eLength);
            if ((t == PacketTypes.Control) && (c == PacketCodes.AuthChallenge) && (dLength > 0))
            {
                inBuffer = new byte[eLength];
                ReceiveData(ref inBuffer, eLength);
                if (mEncrypted)
                {
                    inBuffer = mCrypto.Decrypt(inBuffer);
                }
                token = BitConverter.ToInt32(inBuffer, 0);
                System.Diagnostics.Debug.WriteLine(String.Format("Client Received Challenge Auth Token: {0}", token));
            }
            else
            {
                mAuthenticated = false;
                return false;
            }

            // (4) Send Challenge Response
            byte[] responseBuffer = new byte[HEADER_SIZE + eLength + AUTH_RESPONSE_SIZE];
            WritePacketHeader(ref responseBuffer, PacketTypes.Control, PacketCodes.AuthResponse, dLength + AUTH_RESPONSE_SIZE, eLength + AUTH_RESPONSE_SIZE);
            byte[] sendData = new byte[eLength + AUTH_RESPONSE_SIZE];    // [token (8) | username (16) | password (16)]
            
            Buffer.BlockCopy(BitConverter.GetBytes(token), 0, sendData, 0, sizeof(int));
            UTF8Encoding.UTF8.GetBytes(Username, 0, Math.Min(AUTH_USER_SIZE, Username.Length), sendData, dLength);
            UTF8Encoding.UTF8.GetBytes(Password, 0, Math.Min(AUTH_PASS_SIZE, Password.Length), sendData, dLength + AUTH_PASS_OFFSET);
            if (mEncrypted)
            {
                sendData = mCrypto.Encrypt(sendData);
            }
            Buffer.BlockCopy(sendData, 0, responseBuffer, HEADER_SIZE, sendData.Length);
            mSocket.Send(responseBuffer);

            // (6) Receive Authorization Result
            challengeHeader = new byte[HEADER_SIZE];
            ReceiveHeader(ref challengeHeader, out t, out c, out dLength, out eLength);
            if ((t == PacketTypes.Control) && (c == PacketCodes.AuthOK) && (dLength > 0))
            {
                inBuffer = new byte[eLength];
                ReceiveData(ref inBuffer, eLength);
                if (mEncrypted)
                {
                    inBuffer = mCrypto.Decrypt(inBuffer);
                }
                int newToken = BitConverter.ToInt32(inBuffer, 0);
                System.Diagnostics.Debug.WriteLine(String.Format("Client Received Response Auth Token: {0}", newToken));

                if (newToken == token)
                {
                    mAuthenticated = true;
                }
                else
                {
                    mAuthenticated = false;
                }
            }
            else
            {
                mAuthenticated = false;
                return false;
            }

            return mAuthenticated;
        }
        #endregion

        #region Packet Manipulation
        private void WritePacketHeader(ref byte[] buffer, PacketTypes type, PacketCodes code, int DataLen, int EncryptedLen)
        {
            Buffer.BlockCopy(BitConverter.GetBytes((int)type), 0, buffer, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes((int)code), 0, buffer, 4, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(DataLen), 0, buffer, 8, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(EncryptedLen), 0, buffer, 12, sizeof(int));
        }
        private void ReadPacketHeader(ref byte[] buffer, out PacketTypes type, out PacketCodes code, out int DataLen, out int EncryptedLen)
        {
            int t = BitConverter.ToInt32(buffer, 0);
            int c = BitConverter.ToInt32(buffer, 4);
            int l = BitConverter.ToInt32(buffer, 8);
            int e = BitConverter.ToInt32(buffer, 12);

            type = (PacketTypes)t;
            code = (PacketCodes)c;
            DataLen = l;
            EncryptedLen = e;
        }
        private void ReceiveHeader(ref byte[] buffer, out PacketTypes type, out PacketCodes code, out int DataLen, out int EncryptedLen)
        {
            int received = 0;
            while (received < HEADER_SIZE)
            {
                received += mSocket.Receive(buffer, received, HEADER_SIZE - received, SocketFlags.None);
            }
            ReadPacketHeader(ref buffer, out type, out code, out DataLen, out EncryptedLen);
        }
        #endregion

        private void ReceiveData(ref byte[] buffer, int len)
        {
            int received = 0;
            while (received < len)
            {
                received += mSocket.Receive(buffer, received, len - received, SocketFlags.None);
            }
        }
        private void Send(byte[] data, PacketTypes pt, PacketCodes cc)
        {
            byte[] outBuffer = null;
            if (mEncrypted)
            {
                byte[] cipher = mCrypto.Encrypt(data);
                outBuffer = new byte[HEADER_SIZE + cipher.Length];
                WritePacketHeader(ref outBuffer, pt, cc, data.Length, cipher.Length);
                Buffer.BlockCopy(cipher, 0, outBuffer, HEADER_SIZE, cipher.Length);
            }
            else
            {
                outBuffer = new byte[HEADER_SIZE + data.Length];
                WritePacketHeader(ref outBuffer, pt, cc, data.Length, data.Length);
                Buffer.BlockCopy(data, 0, outBuffer, HEADER_SIZE, data.Length);
            }
            mSocket.Send(outBuffer);
        }
        
        private void StartReceiver()
        {
            mReceiverThread = new Thread(new ThreadStart(ClientReceiverThread));
            mReceiverThread.Name = String.Format("{0} Socket Receiver Thread (ID: {1})", (this.ServerSide ? "Server" : "Client"), this.ID);
            mReceiverThread.Start();
        }
        private void StopReceiver()
        {
            if (mReceiverThread != null)
            {
                mReceiverThread.Join(2000);
                mReceiverThread.Abort();
            }
        }
        private void ClientReceiverThread()
        {
            while (this.Connected)
            {
                try
                {
                    byte[] headerBuffer = new byte[HEADER_SIZE];
                    PacketTypes packetType;
                    PacketCodes controlCode;
                    int dataLength;
                    int encryptedLength;
                    ReceiveHeader(ref headerBuffer, out packetType, out controlCode, out dataLength, out encryptedLength);
                    if ((packetType == PacketTypes.Data) && (encryptedLength > 0))
                    {
                        switch (controlCode)
                        {
                            case PacketCodes.Raw:
                                // Raw Data
                                ReceiveRawData(dataLength, encryptedLength);
                                break;
                            case PacketCodes.FileData:
                                // File Data
                                ReceiveFileData(dataLength, encryptedLength);
                                break;
                            default:
                                // Read and Discard
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // On any error, we have to disconnect -- no way to recover
                    if (OnError != null) OnError(this.ID, ex.Message);
                    if (OnDisconnected != null) OnDisconnected(this.ID);
                }
                finally { }
            }
        }

        private void ReceiveRawData(int dataLength, int encryptedLength)
        {
            byte[] inBuffer = new byte[encryptedLength];
            ReceiveData(ref inBuffer, encryptedLength);
            if (mEncrypted)
            {
                byte[] eBuffer = mCrypto.Decrypt(inBuffer);
                inBuffer = new byte[dataLength];
                Buffer.BlockCopy(eBuffer, 0, inBuffer, 0, dataLength);

            }
            if (OnPacketReceived != null) OnPacketReceived(this.ID, inBuffer);
            inBuffer = null;
        }

        /*********************************************************************/

        private void ReceiveFileData(int dataLength, int encryptedLength)
        {
            // Receive 256 bytes of file name
            byte[] fnameBuffer = new byte[256];
            ReceiveData(ref fnameBuffer, 256);
            string fname = UTF8Encoding.UTF8.GetString(fnameBuffer).Trim('\0');
            FileInfo ReceivedFileInfo = new FileInfo(fname);

            BinaryWriter writer = new BinaryWriter(new FileStream(ReceivedFileInfo.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));

            // Receive 'encryptedLength' of file data in 2KB chunks
            int recvd = 0;
            int written = 0;
            while (recvd < encryptedLength)
            {
                byte[] inBuffer = new byte[2048];
                int toRecv = Math.Min(2048, encryptedLength - recvd);
                int toWrite = Math.Min(2048, dataLength - written);
                ReceiveData(ref inBuffer, toRecv);
                if (mEncrypted)
                {
                    inBuffer = mCrypto.Decrypt(inBuffer);
                }
                if (toWrite < inBuffer.Length)
                {
                    byte[] writeBuffer = new byte[toWrite];
                    Buffer.BlockCopy(inBuffer, 0, writeBuffer, 0, toWrite);
                    inBuffer = writeBuffer;
                }
                writer.Write(inBuffer);
                written += toWrite;
                recvd += toRecv;
            }
            writer.Close();

            if (OnFileReceived != null) OnFileReceived(this.ID, ReceivedFileInfo);
            try { ReceivedFileInfo.Delete(); }
            catch { }
        }
        public void Send(byte[] data)
        {
            Send(data, PacketTypes.Data, PacketCodes.Raw);
        }

        public void Send(string data)
        {
            Send(UTF8Encoding.UTF8.GetBytes(data));
        }

        public void Send(FileInfo fi)
        {
            int totalSize = (int)fi.Length;
            int excess = totalSize & 16;
            int encryptedSize = ((excess == 0) && mEncrypted ? totalSize : totalSize + 16 - excess);

            byte[] sendHeader = new byte[HEADER_SIZE + 256];
            WritePacketHeader(ref sendHeader, PacketTypes.Data, PacketCodes.FileData, totalSize, encryptedSize);
            Buffer.BlockCopy(UTF8Encoding.UTF8.GetBytes(fi.Name), 0, sendHeader, HEADER_SIZE, fi.Name.Length);
            mSocket.Send(sendHeader);

            BinaryReader reader = new BinaryReader(new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
            reader.BaseStream.Position = 0;

            int read = 0;
            while (read < totalSize)
            {
                byte[] sendBuffer = reader.ReadBytes(2048);
                read += sendBuffer.Length;
                if (mEncrypted)
                {
                    sendBuffer = mCrypto.Encrypt(sendBuffer);
                }
                mSocket.Send(sendBuffer);
            }
            reader.Close();
        }
    }
}
