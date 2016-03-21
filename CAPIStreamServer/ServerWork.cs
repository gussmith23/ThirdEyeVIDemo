using CAPIStreamCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace CAPIStreamServer
{

    public delegate byte[] ImageWork(SocketData s);
    class ServerWork
    {
        string m_VideoCaptureFilename;
        Capture m_VideoCaptureInterface;
        int m_VideoCaptureFrameCount;
        private Mat m_FrameMat;

        public void InitVideoCapture(string path)
        {
            try
            {
                m_FrameMat = new Mat();
                m_VideoCaptureFilename = path;
                m_VideoCaptureInterface = null;
                m_VideoCaptureInterface = new Capture(m_VideoCaptureFilename);
                m_VideoCaptureInterface.SetCaptureProperty(CapProp.FrameHeight, 640);
                m_VideoCaptureInterface.SetCaptureProperty(CapProp.FrameWidth, 360);
                m_VideoCaptureInterface.SetCaptureProperty(CapProp.Fps, 5);
                m_VideoCaptureInterface.ImageGrabbed += VideoCaptureInterface_ImageGrabbed;
                m_VideoCaptureFrameCount = (int)m_VideoCaptureInterface.GetCaptureProperty(CapProp.FrameCount);
                m_VideoCaptureInterface.Start();
            }
            catch (Exception e)
            {
            }
        }
        private void VideoCaptureInterface_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                if (!m_VideoCaptureInterface.Retrieve(m_FrameMat, 0))
                    return;

                Image<Bgr, byte> frame = m_FrameMat.ToImage<Bgr, byte>();

                if (frame != null)
                {
                    frame = frame.Resize(640, 360, Inter.Cubic);
                    Process(frame);
                    Thread.Sleep(25);
                    //Application.Current.Dispatcher.Invoke(new Action(() => Display(frame.Convert<Bgra, byte>())));
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }

            
            m_VideoCaptureFrameCount--;

            Console.WriteLine(m_VideoCaptureFrameCount);

            if (m_VideoCaptureFrameCount <= 0)
            {
            }
        }

        private void Process(Image<Bgr, byte> frame)
        {            
            CvInvoke.Imshow("frame", frame);
            CvInvoke.WaitKey(1);
            Console.WriteLine("Byte[] Length = {0}", (uint)frame.Bytes.Length);
            SocketData sendPacket1 = new SocketData(PacketType.VIDEO_FRAME, clientID, 13, 14, (uint)frame.Bytes.Length, frame.Bytes);
            CAPINetworkUtility.sendDataPacket(socket, sendPacket1);
        }



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

            //InitVideoCapture(@"C:\Users\Peter A. Zientara\Documents\Projects\CAPIStreamMaster\CAPIStreamCSharp\CAPIStream\CAPIServerTest\Saft-scrub-v1.mp4");
            while (doWork)
            {/*
                Image<Bgra, Byte> im = new Image<Bgra, Byte>(@"C:\Users\Peter A. Zientara\Documents\Projects\CAPIStreamMaster\CAPIStreamCSharp\CAPIStream\CAPIServerTest\red_Car.jpg");
                im = im.Resize(640, 360, Emgu.CV.CvEnum.Inter.Cubic);
                CvInvoke.Imshow("frame", im);
                CvInvoke.WaitKey(1);
                Console.WriteLine("Byte[] Length = {0}", im.Bytes.Length);
                SocketData sendPacket1 = new SocketData(PacketType.VIDEO_FRAME, clientID, 13, 14, (uint)im.Bytes.Length, im.Bytes);
                CAPINetworkUtility.sendDataPacket(socket, sendPacket1);
                */
                //continue;
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
                SocketData returnPacket = null;
                if(retData != null)
                {
                    switch (dataPacket.message_type)
                    {
                        case PacketType.VIDEO_FRAME:
                            {
                                returnPacket = new SocketData(PacketType.VIDEO_FRAME, clientID, 13, 14, (uint)retData.Length, retData);
                                break;
                            }
                        case PacketType.MODEL_KEYPOINT_EXTRACT:
                            {
                                returnPacket = new SocketData(PacketType.KEYPOINTS, clientID, 13, 14, (uint)retData.Length, retData);
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
                    returnPacket = new SocketData(PacketType.WORK_ACK, clientID, 43, 44, 0);
                }
                CAPINetworkUtility.sendDataPacket(socket, returnPacket);
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
