using CAPIStreamCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CAPIStreamServer
{

    public delegate byte[] ImageWork(SocketData s);
    class ServerWork
    {
        // Client  socket.
        public Socket socket = null;
        public uint clientID = 0;
        Dictionary<PacketType, ImageWork> workFunctions = new Dictionary<PacketType, ImageWork>();
        Thread workThread;   
        public void startWork(Socket socket)
        {
            this.socket = socket;
            workThread = new Thread(() => runConnection(this.socket));
            workThread.Start();            // Start the thread
        }
        public void registerFunctions(Dictionary<PacketType, ImageWork> work)
        {
            this.workFunctions = work;
        }
        public void stopWork()
        {
            workThread.Abort();
            socket.Close();
        }
        public void runConnection(Socket socket) {
            Console.WriteLine("Server thread started! Client ID: {0}", this.clientID);
            AcceptConnection(socket);
            SocketData dataPacket;
            byte[] img_data;
            Boolean doWork = true;
            while (doWork)
            {
                dataPacket = CAPINetworkUtility.receiveDataPacket(socket);
                if(dataPacket == null)
                {
                    socket.Close();
                    doWork = false;
                    return;
                }
                byte[] retData = null;
                //Delegate based calls
                if (workFunctions.ContainsKey(dataPacket.message_type))
                {
                    ImageWork del = workFunctions[dataPacket.message_type];
                    retData = del(dataPacket);
                }
                if(retData != null)
                {
                    switch (dataPacket.message_type)
                    {
                        case PacketType.VIDEO_FRAME:
                            {
                                SocketData sendPacket = new SocketData(PacketType.KEYPOINT_MATCHES, clientID, 13, 14, (uint)retData.Length, retData);
                                CAPINetworkUtility.sendDataPacket(socket, sendPacket);
                                break;
                            }
                        case PacketType.MODEL_KEYPOINT_EXTRACT:
                            {
                                SocketData sendPacket = new SocketData(PacketType.KEYPOINTS, clientID, 13, 14, (uint)retData.Length, retData);
                                CAPINetworkUtility.sendDataPacket(socket, sendPacket);
                                break;
                            }
                        case PacketType.MODELS_DONE:
                            {
                                //models_finished();
                                break;
                            }
                        case PacketType.CONNECTION_DISCONNECT:
                            {
                                //client_count--;
                                //close(socketfd);
                                doWork = false;
                                break;
                            }
                        default:
                            {
                                //do nothing??
                                break;
                            }
                    }
                }
                else
                {
                    SocketData sendPacket = new SocketData(PacketType.WORK_ACK, clientID, 43, 44, 0);
                    CAPINetworkUtility.sendDataPacket(socket, sendPacket);
                }
                #region old handling of packets
                /*

                switch (dataPacket.message_type)
                {
                    case PacketType.VIDEO_FRAME:
                        {
                            //fprintf(stdout, "Video rows: %i\n", data.message_length);
                            img_data = dataPacket.data;
                            #pragma warning - implement the functionaility
                            Console.WriteLine("Image received, Width:{0}, Height:{1}", dataPacket.message_id, dataPacket.stream_id);
                            //height width
                            //apply emgu there, message_id == x, stream_id == height;
                            //cv::Mat mat = cv::Mat(dataPacket.message_id, dataPacket.stream_id, CV_8UC3, (void*)img_data);
                            //cv::imshow("Transmitted Frame", mat);
                            //cv::waitKey(1);
                            //int length = 0;
                            //uint8_t* data = runSURF(mat, &length, true);
                            //uint8_t * data = NULL;
                            byte[] data = null;
                            uint length = 0;
                            SocketData sendPacket = new SocketData(PacketType.KEYPOINT_MATCHES, 12, 13, 14, length, data);
                            //print_data_packet(&sendPacket);
                            CAPINetworkUtility.sendDataPacket(socket, sendPacket);
                            break;
                        }
                    case PacketType.MODEL_KEYPOINT_EXTRACT:
                        {
                            //fprintf(stdout, "Video rows: %i\n", data.message_length);
                            img_data = dataPacket.data;
#pragma warning - implement the functionaility
                            //height width
                            //apply emgu there, message_id == x, stream_id == height;
                            //cv::Mat mat = cv::Mat(dataPacket.message_id, dataPacket.stream_id, CV_8UC3, (void*)img_data);


                            byte[] data = null;
                            uint length = 0;
                            SocketData sendPacket = new SocketData(PacketType.KEYPOINTS, 12, 13, 14, length, data);
                            //print_data_packet(&sendPacket);
                            CAPINetworkUtility.sendDataPacket(socket, sendPacket);
                            break;
                        }
                    case PacketType.MODELS_DONE:
                        {
                            //models_finished();
                            break;
                        }
                    case PacketType.CONNECTION_DISCONNECT:
                        {
                            //client_count--;
                            //close(socketfd);
                            doWork = false;
                            break;
                        }
                    default:
                        {
                            //do nothing??
                            break;
                        }
                }
                */
                #endregion
            }
        }
        private void AcceptConnection(Socket socket)
        {
            SocketData ap = new SocketData();
            ap.message_type = (UInt32)PacketType.CONNECTION_ACCEPT;
            ap.stream_id = (uint)clientID;
            ap.frame_id = 42;
            ap.message_length = 0;
            ap.message_id = 0;
            ap.data = null;
            CAPINetworkUtility.sendDataPacket(socket, ap);
        }
    }
}
