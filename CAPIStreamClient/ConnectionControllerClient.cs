using System;
using System.Net.Sockets;
using System.Threading;
using CAPIStreamCommon;
using System.Collections.Generic;

namespace CAPIStreamClient
{
    public delegate SocketData PacketProcess(SocketData s);

    public class ConnectionControllerClient
    {
        Dictionary<PacketType, PacketProcess> delegateFunctions = new Dictionary<PacketType, PacketProcess>();
        private Boolean isConnected = false;
        private TcpClient tcpClient;
        private Boolean configured = false;
        private NetworkStream stream;
        private Boolean shutdown = false;
        private uint clientId = 0;
        public ConnectionControllerClient() { }
        public ConnectionControllerClient(ConnectionType type, String host, Int32 port)
        {
            configureConnection(type, host, port);
        }
        ~ConnectionControllerClient() { }
        public void configureConnection(ConnectionType type, String host, Int32 port)
        {
            switch (type)
            {
                case ConnectionType.UDP:
                    {
                        break;
                    }
                case ConnectionType.TCP:
                    {
                        tcpClient = new TcpClient(host, port);
                        stream = tcpClient.GetStream();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            this.configured = true;
        }
        public void Shutdown()
        {
            if(stream != null)
            {
                stream.Close();
            }
            if(tcpClient != null)
            {
                tcpClient.Close();
            }
        }
        public Boolean isConfigured()
        {
            return configured;
        }
        public SocketData receiveDataPacket() {
            SocketData data;
            data = ReceiveDataPacket(stream);
            if(data != null)
            {
                switch (data.message_type)
                {
                    case PacketType.ROI_FRAME_INFO:
                        {
                            return data;
                        }
                    case PacketType.CONNECTION_DISCONNECT:
                        {
                            Console.WriteLine("Connection Disconnected");
                            shutdown = true;
                            return null;
                        }
                    case PacketType.CONNECTION_ACCEPT:
                        {
                            this.clientId = data.stream_id; //set client id
                            Console.WriteLine("Connection Accepted, Client ID: {0}", this.clientId);
                            shutdown = false;
                            return data;
                        }
                    case PacketType.KEYPOINTS:
                        {
                            Console.WriteLine("Recvied Keypoints -- Length: {0}", data.message_length);
                            return data;
                            break;
                        }
                    case PacketType.KEYPOINT_MATCHES:
                        {
                            Console.WriteLine("Recvied Keypoints -- Length: {0}", data.message_length);
                            return data;
                            break;
                        }
                    default:
                        {
                            return data;
                        }
                }
            }
            else
            {
                return null;
            }
        }
        public void sendDataPacket(SocketData data)
        {
            data.stream_id = clientId; //append clientID; 
            sendData(data.toByteArray());
        }
        private SocketData ReceiveDataPacket(NetworkStream stream)
        {
            byte[] header = readStream(stream, (UInt32)SocketData.getHeaderSize());
            if(header != null)
            {
                SocketData d = new SocketData(header);
                if (d.message_length > 0)
                {
                    byte[] body = readStream(stream, d.message_length);
                    d.data = body;
                }
                return d;
            }
            else
            {
                Shutdown();
                return null;
            }
        }
        //wrapper for read stream to block untill expected number of bytes is returned
        private byte[] readStream(NetworkStream stream, UInt32 length)
        {
            try
            {
                byte[] data = new byte[length];
                int bytes_read = 0;
                int idx = 0;
                int remaining = (int)length;
                while (remaining != 0)
                {
                    bytes_read = stream.Read(data, idx, remaining);
                    remaining -= bytes_read;
                    idx += bytes_read;
                }
                return data;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private void sendData(Byte[] data)
        {
            if (stream.CanWrite)
            {
                stream.Write(data, 0, data.Length);
            }
            else //stream closed
            {
                Shutdown();
            }
        }

        public void recieveDataPacketAsync()
        {
            Thread listenThread = new Thread(new ThreadStart(recievePacketAsync));
            listenThread.Start();
        }
        private void recievePacketAsync()
        {
            SocketData pkt = receiveDataPacket();
            socketDataDispatch(pkt);
        }
        private void socketDataDispatch(SocketData pkt)
        {
            SocketData returnPacket = null;
            if (delegateFunctions.ContainsKey(pkt.message_type))
            {
                PacketProcess del = delegateFunctions[pkt.message_type];
                returnPacket = del(pkt);
            }
            if (returnPacket != null)
            {
                sendDataPacket(returnPacket);
            }
         }
        public void registerDelegate(PacketType type, PacketProcess func)
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