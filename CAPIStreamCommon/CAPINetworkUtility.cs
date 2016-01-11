using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CAPIStreamCommon
{
    public class CAPINetworkUtility
    {
        static private byte[] receiveLength(Socket socket, UInt32 length)
        {
            try
            {
                byte[] data = new byte[length];
                int bytes_read = 0;
                int idx = 0;
                int remaining = (int)length;
                while (remaining != 0)
                {
                    bytes_read = socket.Receive(data, idx, remaining, SocketFlags.None);
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
        static public void sendDataPacket(Socket socket, SocketData data)
        {
            socket.Send(data.toByteArray());
        }
        static public SocketData receiveDataPacket(Socket socket)
        {
            SocketData d;
            byte[] header = receiveLength(socket, (UInt32)SocketData.getHeaderSize());
            if (header != null)
            {
                d = new SocketData(header);
                if (d.message_length > 0)
                {
                    byte[] body = receiveLength(socket, d.message_length);
                    d.data = body;
                }
            }
            else
            {
                d = null;
            }
            return d;
        }
    }
}
