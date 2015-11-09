using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientServer
{
    enum PacketTypes : int
    {
        Data = 0x69,    // 0110 1001
        Control = 0xA5  // 1010 0101
    }

    enum PacketCodes : int
    {
        Raw = 0x00,                // 0000 0000
        KeyExchange = 0xA1,         // 1010 0001
        KeyOK = 0xA3,               // 1010 0011
        KeyFailure = 0xA7,          // 1010 0111
        AuthChallenge = 0xC1,       // 1100 0001
        AuthResponse = 0xC3,        // 1100 0011
        AuthReject = 0xC7,          // 1100 0111
        AuthOK = 0xCF,              // 1100 1111
        FileData = 0xFD             // 1111 1101
    }

    class ConnectInfo
    {
        public string Host { get; private set; }
        public int Port { get; private set; }
        public string User { get; private set; }
        public string Pass { get; private set; }

        public ConnectInfo(string Host, int Port, string User, string Pass)
        {
            this.Host = Host;
            this.Port = Port;
            this.User = User;
            this.Pass = Pass;
        }
    }
}
