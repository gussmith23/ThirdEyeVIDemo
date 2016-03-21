

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;

using CAPIStreamCommon;
using CAPIStreamClient;
using Emgu;
using Emgu.CV;
using System.Windows.Forms;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace CAPIStreamTest
{
    class Program
    {

        static Object lc;
        static SocketData recvData;
        static Capture videoCaptureInterface = null;
        static ConnectionControllerClient client;
        static void Main(string[] args)
        {
            lc = new Object();
            System.Console.WriteLine("Hello World");
            //byte[] img_data = read_byte_array_file(@"C:\Users\Peter A. Zientara\Documents\Projects\CAPIStream\Csharp\CAPIStream\CAPIStreamTest\test_gabor_input.dat");
            //System.Console.WriteLine("{0}", img_data.Length);
            client = new ConnectionControllerClient();
            client.configureConnection(ConnectionType.TCP, "localhost", 2275); //PORT CAPI SPELLED IN PHONE DIALER
            SocketData recData = client.receiveDataPacket(); //recieve header packet
            if (recData == null)
            {
                System.Console.WriteLine("good!");
            }

            #region do the main work
            //InitVideoCapture();

            //run();


            #endregion
            System.Console.ReadLine();
        }
        /*

        static void InitVideoCapture()
        {

            try
            {
                videoCaptureInterface = null;
                videoCaptureInterface = new Capture(@"C:\Users\Peter A. Zientara\Videos\Saft-scrub-v1.mp4");
                videoCaptureInterface.SetCaptureProperty(CapProp.FrameHeight, 720);
                videoCaptureInterface.SetCaptureProperty(CapProp.FrameWidth, 1280);
                videoCaptureInterface.SetCaptureProperty(CapProp.Fps, 20);


                videoCaptureInterface.ImageGrabbed += VideoCaptureInterface_ImageGrabbed;

                //FrameRate = videoCaptureInterface.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS);
                //TotalFrames = videoCaptureInterface.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_COUNT);
                videoCaptureInterface.Start();
                //videoCaptureTimer.Stop();
                //videoCaptureTimer.Tick += videoCaptureTimer_Tick;
                //videoCaptureTimer.Interval = new TimeSpan(300000);
                //videoCaptureTimer.Start();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        static private void run()
        {
            Image<Bgr, byte>[] resultImages;
            Mat frameMat = new Mat();
            Image<Bgr, byte> frame;

            try
            {
                videoCaptureInterface.Grab();
                while ((videoCaptureInterface.Retrieve(frameMat, 0)))
                {
                    if (!frameMat.IsEmpty) {
                        frame = frameMat.ToImage<Bgr, byte>().Resize(1280, 720, Inter.Cubic);
                    //Image<Bgr, byte> frame = new Image<Bgr, byte>(@"C:\Users\Kevin\Pictures\NDVISample.png");//.Resize(800, 600, Inter.Cubic,false);
                    //Image<Bgr, byte> frame = new Image<Bgr, byte>(@"C:\Users\Kevin\Pictures\NDVISample.png");//.Resize(800, 600, Inter.Cubic,false);
                    //Image<Bgr, byte> frame = new Image<Bgr, byte>(@"C:\Users\Kevin\Pictures\NSW.png");//.Resize(800, 600, Inter.Cubic,false);
                    //Image<Bgr, byte> frame = new Image<Bgr, byte>(@"C:\Users\Kevin\Pictures\walnut_NDVI_closer.jpg");//.Resize(800, 600, Inter.Cubic,false);

                    if (frame != null)
                    {
                        byte[] x = new byte[frame.Width * frame.Height * 3];
                        Buffer.BlockCopy(frame.Data, 0, x, 0, x.Length);

                        SocketData data = new SocketData((UInt32)PacketType.VIDEO_FRAME, (UInt32)frame.Width, (UInt32)frame.Height, 42, (UInt32)x.Length);
                        data.data = x;

                        client.sendDataPacket(data);
                        recvData = client.receiveData();
                        
                        if (recvData != null)
                        {
                            if (recvData.message_type == (uint)PacketType.KEYPOINTS)
                            {
                                SURFKeypoint[] a = SURFKeypoint.extract_keypoints(recvData.data, (int)recvData.message_length);

                                foreach (SURFKeypoint kp in a)
                                {
                                    frame.Draw(new CircleF(new PointF(kp.c1, kp.r1), kp.scale), new Bgr(0, 0, 255), 1);
                                }

                                CvInvoke.Imshow("test", frame);
                                CvInvoke.WaitKey(1);
                            }
                            if (recvData.message_type == (uint)PacketType.KEYPOINT_MATCHES)
                            {
                                SURFKeypointMatches[] a = SURFKeypointMatches.extract_keypoint_matches(recvData.data, (int)recvData.message_length);
                                #warning do something with the returned matches
                            }
                        }
                        

                    }
                    }
                }
                

            }
            catch (Exception ex)
            {
                //Console.WriteLine(recvData.message_length);
                MessageBox.Show(ex.Message);
            }
        }

        private static void VideoCaptureInterface_ImageGrabbed(object sender, EventArgs e)
        {
                Image<Bgr, byte>[] resultImages;
                Mat frameMat = new Mat();

                try
                {
                    if (!videoCaptureInterface.Retrieve(frameMat, 0))
                        return;

                Image<Bgr, byte> frame = frameMat.ToImage<Bgr, byte>().Resize(1280, 720, Inter.Cubic).Clone();
                
                    //Image<Bgr, byte> frame = new Image<Bgr, byte>(@"C:\Users\Kevin\Pictures\NDVISample.png");//.Resize(800, 600, Inter.Cubic,false);
                    //Image<Bgr, byte> frame = new Image<Bgr, byte>(@"C:\Users\Kevin\Pictures\NDVISample.png");//.Resize(800, 600, Inter.Cubic,false);
                    //Image<Bgr, byte> frame = new Image<Bgr, byte>(@"C:\Users\Kevin\Pictures\NSW.png");//.Resize(800, 600, Inter.Cubic,false);
                    //Image<Bgr, byte> frame = new Image<Bgr, byte>(@"C:\Users\Kevin\Pictures\walnut_NDVI_closer.jpg");//.Resize(800, 600, Inter.Cubic,false);

                    if (frame != null)
                    {
                        byte[] x = new byte[frame.Width * frame.Height * 3];
                        Buffer.BlockCopy(frame.Data, 0, x, 0, x.Length);

                        SocketData data = new SocketData((UInt32)PacketType.VIDEO_FRAME, (UInt32)frame.Width, (UInt32)frame.Height, 42, (UInt32)x.Length);
                        data.data = x;
                        
                        client.sendDataPacket(data);
                        recvData = client.receiveData();
                        
                        if (recvData != null)
                        {
                            if (recvData.message_type == (uint)PacketType.KEYPOINTS)
                            {
                                SURFKeypoint[] a = SURFKeypoint.extract_keypoints(recvData.data, (int)recvData.message_length);

                                foreach (SURFKeypoint kp in a)
                                {
                                    frame.Draw(new CircleF(new PointF(kp.c1, kp.r1), kp.scale), new Bgr(0, 0, 255), 1);
                                }

                                CvInvoke.Imshow("test", frame);
                                CvInvoke.WaitKey(1);
                            }
                        }
                        

                    }
                
                }
                catch (Exception ex)
                {
                    Console.WriteLine(recvData.message_length);
                    MessageBox.Show(ex.Message);
                }

        }

        static public byte[] read_byte_array_file(String filename)
        {
            int length = 0;
            byte[] data;

            if (File.Exists(filename))
            {
                StreamReader file = null;
                try
                {
                    file = new StreamReader(filename);
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        length++;
                    }
                    file.Close();
                    file = new StreamReader(filename);
                    data = new byte[length];
                    int i = 0;
                    while ((line = file.ReadLine()) != null)
                    {
                        data[i++] = (byte)UInt16.Parse(line);
                    }
                }
                finally
                {
                    if (file != null)
                        file.Close();
                }
                return data;
            }
            else
            {
                return null;
            }
        }
        */
    }
}
