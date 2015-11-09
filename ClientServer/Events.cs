using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ClientServer.Events
{
    public delegate void ListenerStartedDelegate();
    public delegate void ServerErrorDelegate(string ErrorMessage);
    public delegate void ListenerStoppedDelegate();

    public delegate void ClientConnectedDelegate(int ConnectionID);
    public delegate void ClientDisconnectedDelegate(int ConnectionID);
    public delegate void ClientErrorDelegate(int ConnectionID, string ErrorMessage);
    public delegate void ClientPacketReceivedDelegate(int ConnectionID, byte[] data);
    public delegate void ClientFileReceivedDelegate(int ConnectionID, FileInfo fiTempFile);
    
    public delegate void ConnectedDelegate();
    public delegate void DisconnectedDelegate();
    public delegate void ErrorDelegate(string ErrorMessage);
    public delegate void PacketReceivedDelegate(int ConnectionID, byte[] data);

}
