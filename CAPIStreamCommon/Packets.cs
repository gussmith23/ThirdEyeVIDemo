using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAPIStreamCommon
{

    public enum PacketType : uint
    {
        CONNECTION_ACCEPT = 0x0,
        CONNECTION_DISCONNECT = 0x1,
        OPEN_STREAM = 0x2,
        END_OF_STREAM = 0x3,
        REINITIALIZE_STREAM = 0x4,
        STREAM_ID_MESG = 0x5,
        VIDEO_FRAME = 0x6,
        ROI_FRAME_INFO = 0x7,
        KEYPOINTS = 0x8,
        KEYPOINT_MATCHES = 0x9,
        MODEL_KEYPOINT_EXTRACT = 0xA,
        MODELS_DONE = 0xB,
        WORK_ACK = 0xC,
        REGISTER_TASK = 0xD,
        REGISTER_TASK_REPLY = 0xE,
        ROI_MATCH = 0x0F,
        GESTURE_LEFT = 0x12,
        GESTURE_RIGHT = 0x13,
        GESTURE_UP = 0x14,
        GESTURE_DOWN = 0x15,
        GESTURE_SELECT = 0x16,
        GESTURE_BACK = 0x17,
        OBJECT = 0x19,
        INVALID_PACKET = 0xAA,
        SHOW_DETAIL_VIEW = 0x1B,
        HIDE_DETAIL_VIEW = 0x1C,
        MODEL_CHANGE = 0x1D

    }
    public class SocketData
    {
        public PacketType message_type { get; set; }
        public UInt32 stream_id { get; set; }
        public UInt32 message_id { get; set; }
        public UInt32 frame_id { get; set; }
        public UInt32 message_length { get; set; }
        public byte[] data { get; set; }
        public SocketData() { }
        public SocketData(byte[] data)
        {
            if (data != null && data.Length >= SocketData.getHeaderSize())
            {
                message_type = (PacketType)((((((data[3] << 8) | data[2]) << 8) | data[1]) << 8) | data[0]);
                stream_id = (UInt32)((((((data[7] << 8) | data[6]) << 8) | data[5]) << 8) | data[4]);
                message_id = (UInt32)((((((data[11] << 8) | data[10]) << 8) | data[9]) << 8) | data[8]);
                frame_id = (UInt32)((((((data[15] << 8) | data[14]) << 8) | data[13]) << 8) | data[12]);
                message_length = (UInt32)((((((data[19] << 8) | data[18]) << 8) | data[17]) << 8) | data[16]);

                int dataLength = data.Length;
                int remainingLength = (dataLength - SocketData.getHeaderSize());
                if (message_length <= remainingLength)
                {
                    this.data = new byte[remainingLength];
                    Array.Copy(data, SocketData.getHeaderSize(), this.data, 0, remainingLength);
                }
                else
                {
                    this.data = null;
                }
            }
        }
        static public int getHeaderSize()
        {
            return 20;
        }
        public SocketData(PacketType message_type, UInt32 stream_id, UInt32 message_id, UInt32 frame_id, UInt32 message_length, byte[] data = null)
        {
            this.message_type = message_type;
            this.stream_id = stream_id;
            this.message_id = message_id;
            this.message_length = message_length;
            this.frame_id = frame_id;
            this.data = data;
            if (data == null)
            {
                this.message_length = 0;
            }
            else
            {
                this.message_length = (uint)data.Length;
            }
        }
        public SocketData(PacketType message_type, UInt32 stream_id, UInt32 message_id, UInt32 frame_id, byte[] data = null)
        {
            this.message_type = message_type;
            this.stream_id = stream_id;
            this.message_id = message_id;
            this.frame_id = frame_id;
            this.data = data;
            if (data == null)
            {
                this.message_length = 0;
            }
            else
            {
                this.message_length = (uint)data.Length;
            }
        }
        public byte[] toByteArray()
        {
            byte[] data = new byte[SocketData.getHeaderSize() + this.message_length];
            uint mt = (uint)message_type;
            data[0] = (byte)mt;
            data[1] = (byte)(mt >> 8);
            data[2] = (byte)(mt >> 16);
            data[3] = (byte)(mt >> 24);

            data[4] = (byte)stream_id;
            data[5] = (byte)(stream_id >> 8);
            data[6] = (byte)(stream_id >> 16);
            data[7] = (byte)(stream_id >> 24);

            data[8] = (byte)message_id;
            data[9] = (byte)(message_id >> 8);
            data[10] = (byte)(message_id >> 16);
            data[11] = (byte)(message_id >> 24);

            data[12] = (byte)frame_id;
            data[13] = (byte)(frame_id >> 8);
            data[14] = (byte)(frame_id >> 16);
            data[15] = (byte)(frame_id >> 24);

            data[16] = (byte)message_length;
            data[17] = (byte)(message_length >> 8);
            data[18] = (byte)(message_length >> 16);
            data[19] = (byte)(message_length >> 24);

            if ((this.data != null) && (this.message_length != 0))
            {
                Array.Copy(this.data, 0, data, SocketData.getHeaderSize(), this.message_length);
            }
            return data;
        }
    }
}
