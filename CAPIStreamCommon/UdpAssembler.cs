using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CAPIStreamCommon
{
    class H264_Nal_Packet
    {
        int numberOfSegments = 0;
        byte[][] packetPieces;
        int totalLength;
        bool complete = true;
        uint numReceived = 0;
        public H264_Nal_Packet(int numberOfPieces, int segmentNumber, byte[] data)
        {
            this.numberOfSegments = numberOfPieces;
            totalLength = 0;
            complete = false;
            packetPieces = new byte[numberOfSegments][];
            addSegment(segmentNumber, data);
        }
        public byte[] addSegment(int segmentNumber, byte[] data)
        {
            if (packetPieces[segmentNumber].Length == 0 || packetPieces[segmentNumber] == null) {
                packetPieces[segmentNumber] = data;
                totalLength += data.Length;
                numReceived++;
            }
            else
            {
                totalLength -= packetPieces[segmentNumber].Length;
                packetPieces[segmentNumber] = data;
                totalLength += data.Length;
            }
            if(numReceived == numberOfSegments)
            {
                return assembleFullNal();
            }
            else
            {
                return null;
            }
        }
        private byte[] assembleFullNal()
        {
            byte[] arr = new byte[totalLength];
            int dest = 0;
            for (int i = 0; i < numberOfSegments; ++i){
                packetPieces[i].CopyTo(arr, dest);
                Array.Copy(packetPieces[i], 0, arr, dest, packetPieces[i].Length);
                dest += packetPieces[i].Length;
            }
            return arr;
        }
    }
    //stream_id - nal number
    //frame_id - num pieces
    //message_id - piece number
    class H264_TimeoutFrame
    {
        TimeoutWrapper t;
        public H264_Nal_Packet p { get; }
        public H264_TimeoutFrame(int timeout, int numberOfSegments, int segmentNumber, byte[] data, removeNal callback, int nalNumber)
        {
            t = new TimeoutWrapper(timeout, nalNumber, callback);
            p = new H264_Nal_Packet(numberOfSegments, segmentNumber, data);
        }
    }
    class TimeoutWrapper
    {
        removeNal removal_cb;
        Timer timer;
        int packet_id;
        public TimeoutWrapper(int timeout_ms, int nalNumber, removeNal removal_cb)
        {
            packet_id = nalNumber;
            this.removal_cb = removal_cb;
            timer.Interval = timeout_ms;
            timer.Elapsed += (Object src, ElapsedEventArgs arg) =>
            {
                removal_cb(packet_id);
            };
            timer.Start();
        }
    }
    public delegate void removeNal(int num);
        //stream_id - nal number
    //frame_id - num pieces
    //message_id - piece number
    class UdpAssembler 
    {
        Dictionary<int, H264_TimeoutFrame> live_packets;
        int packet_timeout_ms = 15;
        public UdpAssembler(int timeout_ms)
        {
            live_packets = new Dictionary<int, H264_TimeoutFrame>();
            packet_timeout_ms = timeout_ms;
        }
        public byte[] addDataPacket(SocketData d)
        {
            if(live_packets.ContainsKey((int)d.stream_id)){
                H264_TimeoutFrame tf;
                live_packets.TryGetValue((int)d.stream_id, out tf);
                return tf.p.addSegment((int)d.message_id, d.data);
            }
            else
            {
                live_packets.Add((int)d.stream_id, new H264_TimeoutFrame(packet_timeout_ms, (int)d.frame_id, (int)d.stream_id, d.data, removeFromLive, (int)d.message_id));
                return null;
            }
        }
        void removeFromLive(int num)
        {
            //H264_TimeoutFrame tf;
            //live_packets.TryGetValue(num, out tf);
            live_packets.Remove(num);
        }
    }
    class UdpOrderer
    {
        UInt32 lastNumberReceived;
        UdpAssembler assembler;
        Timer deleteTimer;

        byte[][] complete_nals;
        public UdpOrderer()
        {
            deleteTimer = new Timer();
            deleteTimer.Interval = 5;
            deleteTimer.Elapsed += DeleteTimer_Elapsed;
            deleteTimer.Start();
            assembler = new UdpAssembler(5);
            lastNumberReceived = 0;
        }

        private void DeleteTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //throw new NotImplementedException();
            
        }

        public void addDataPacket(SocketData d)
        {
            assembler.addDataPacket(d);

            return;
        }
    }
}
