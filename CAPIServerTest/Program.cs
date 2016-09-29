using CAPIStreamCommon;
using CAPIStreamServer;
using CAPIStreamClient;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


namespace CAPIServerTest
{
    public class Program
    {
        static int width = 1280;
        static int height = 720;
        static string[] models_names;
        static int model_idx = 0;

        [DllImport(@"C:\Users\Peter A. Zientara\Documents\Projects\CAPIStreamMaster\CAPIStreamCSharp\CAPIStream\x64\Debug\ffmpeg_export.dll")]
        private static extern int decode_packet(IntPtr image, int length, int width, int height);
        [DllImport(@"C:\Users\Peter A. Zientara\Documents\Projects\CAPIStreamMaster\CAPIStreamCSharp\CAPIStream\x64\Debug\ffmpeg_export.dll")]
        private static extern IntPtr getFrame();
        [DllImport(@"C:\Users\Peter A. Zientara\Documents\Projects\CAPIStreamMaster\CAPIStreamCSharp\CAPIStream\x64\Debug\ffmpeg_export.dll")]
        private static extern IntPtr convertYUVtoRGB(IntPtr yuv_data, int width, int height);
        [DllImport(@"C:\Users\Peter A. Zientara\Documents\Projects\CAPIStreamMaster\CAPIStreamCSharp\CAPIStream\x64\Debug\ffmpeg_export.dll")]
        private static extern void initFrameConverter(int width, int height);
        [DllImport(@"C:\Users\Peter A. Zientara\Documents\Projects\CAPIStreamMaster\CAPIStreamCSharp\CAPIStream\x64\Debug\ffmpeg_export.dll")]
        private static extern void deinitFrameConverter();

        static FileStream stream;
        static BinaryWriter writ;

        //use this do demonstrate offload computation;
        static ConnectionControllerClient offload_client;
        static Boolean offload_client_ready = false;

        private static readonly bool h264_encode = true;
        //Create functions to pass as delegate
        static SocketData handleGesture(SocketData d)
        {
            Console.WriteLine(d.message_type);
            model_idx++;
            string s = models_names[model_idx];
            //send register task
            offload_client.sendDataPacket(new SocketData(PacketType.MODEL_CHANGE, 1280, 720, 0, Encoding.ASCII.GetBytes(s)));
            return null;
        }
        static SocketData processFrame(SocketData d)
        {
            //Mat img = new Mat((int)d.stream_id, (int)d.message_id, Emgu.CV.CvEnum.DepthType.Cv8U, 4);
            //CvInvoke.Imdecode(d.data, Emgu.CV.CvEnum.LoadImageType.AnyColor, img);
            //CvInvoke.Imshow("image", img); 
            //writ.Write(d.data);
            #region - Android / Glass Testing
            if (d != null)
            {
                if (h264_encode)
                {
                    byte[] frame_data = null;
                    int decodedFrameLength = 0;
                    unsafe
                    {
                        IntPtr byteArray = Marshal.AllocHGlobal(d.data.Length);
                        Marshal.Copy(d.data, 0, byteArray, d.data.Length);
                        decodedFrameLength = decode_packet(byteArray, d.data.Length, width, height);
                        Marshal.FreeHGlobal(byteArray);
                    }
                    if (decodedFrameLength > 0)
                    {
                        frame_data = new byte[width * height * 4];
                        IntPtr byteArray = getFrame();
                        Marshal.Copy(byteArray, frame_data, 0, width * height * 4);
                        Image<Bgra, byte> x = new Image<Bgra, byte>(width, height);
                        Buffer.BlockCopy(frame_data, 0, x.Data, 0, width * height * 4);
                        //blocking call to offload///// - will combine in future versions
                        offload_client.sendDataPacket(new SocketData(PacketType.VIDEO_FRAME, (uint)width, (uint)height, 0, frame_data));
                        SocketData work_reply = offload_client.receiveDataPacket();
                        ///////////////////////////////
                        if (work_reply.frame_id == 0)
                        {
                            ROIRegion[] r = ROIRegion.extract_regions(work_reply.data, (int)work_reply.message_length);
                            foreach (ROIRegion reg in r)
                            {
                                Point[] contour = new Point[4];

                                contour[0] = new Point((int)reg.x0, (int)reg.y0);
                                contour[1] = new Point((int)reg.x1, (int)reg.y1);
                                contour[2] = new Point((int)reg.x2, (int)reg.y2);
                                contour[3] = new Point((int)reg.x3, (int)reg.y3);
                                x.Draw(contour, new Bgra(0, 0, 255, 255), 3);
                                write_guidance(contour, x);
                            }
                            CvInvoke.Imshow("decoded_frame", x);
                            CvInvoke.WaitKey(1);
                        }
                        else
                        {
                            Console.WriteLine("no good draw");
                            CvInvoke.Imshow("decoded_frame", x);
                            CvInvoke.WaitKey(1);
                        }
                    }
                }
                else
                {
                    /*
                    int size = width * height * 4;
                    byte[] rgb_data = new byte[size];
                    unsafe
                    {
                        IntPtr byteArray = Marshal.AllocHGlobal(d.data.Length);
                        Marshal.Copy(d.data, 0, byteArray, d.data.Length);
                        IntPtr rgb_data_ptr;
                        rgb_data_ptr = convertYUVtoRGB(byteArray, width, height);
                        Marshal.FreeHGlobal(byteArray);
                        Marshal.Copy(rgb_data_ptr, rgb_data, 0, size);
                    }
                    */
                    Image<Gray, Byte> image = new Image<Gray, Byte>(width, height);
                    Buffer.BlockCopy(d.data, 0, image.Data, 0, 640 * 480);
                    CvInvoke.Imshow("frame", image);
                    CvInvoke.WaitKey(1);
                    //                    image.Resize(640, 360, Emgu.CV.CvEnum.Inter.Cubic);
                    //                    return image.Bytes ;
                    return null;
                }

            }

            #endregion
            #region -- Glove Testing
            /*
           if(d != null){
               int size = width * height * 4;
               byte[] rgb_data = new byte[size];
               unsafe
               {
                   IntPtr byteArray = Marshal.AllocHGlobal(d.data.Length);
                   Marshal.Copy(d.data, 0, byteArray, d.data.Length);
                   IntPtr rgb_data_ptr;
                   rgb_data_ptr = convertYUVtoRGB(byteArray, width, height);
                   Marshal.FreeHGlobal(byteArray);
                   Marshal.Copy(rgb_data_ptr, rgb_data, 0, size);
               }
               Image<Bgra, Byte> image = new Image<Bgra, Byte>(width, height);
               Buffer.BlockCopy(rgb_data, 0, image.Data, 0, size);
               CvInvoke.Imshow("frame", image);
               CvInvoke.WaitKey(1);
           }
           */
            #endregion
            return null;
        }
        static SocketData configureOffload(SocketData d)
        {
            SocketData returnData = null;
            if (d.message_type == PacketType.REGISTER_TASK_REPLY)
            {
                offload_client_ready = true;
            }
            model_idx = 0;
            string s = models_names[model_idx];
            //send register task
            returnData = new SocketData(PacketType.MODEL_CHANGE, 1280, 720, 0, Encoding.ASCII.GetBytes(s));
            return returnData;
        }
        static void write_guidance(Point[] points, Image<Bgra, byte> img)
        {
            int avg_x = 0;
            int avg_y = 0;
            foreach (Point p in points)
            {
                avg_x += p.X;
                avg_y += p.Y;
            }
            avg_x = avg_x / 4;
            avg_y = avg_y / 4;
            if (avg_x > ((2 / 3) * img.Rows))
            {
                //they need to move up
                img.Draw("Up", new Point(0, img.Rows), Emgu.CV.CvEnum.FontFace.HersheyPlain, 4.0, new Bgra(0, 0, 255, 255), 3);
            }
            else if (avg_x < ((1 / 3) * img.Rows))
            {
                //they need to move down;
                img.Draw("Down", new Point(0, img.Rows), Emgu.CV.CvEnum.FontFace.HersheyPlain, 4.0, new Bgra(0, 0, 255, 255), 3);
            }
            else
            {
                if (avg_y > ((2 / 3) * img.Cols))
                {
                    //they need to move left
                    img.Draw("Left", new Point(0, img.Rows), Emgu.CV.CvEnum.FontFace.HersheyPlain, 4.0, new Bgra(0, 0, 255, 255), 3);
                }
                else if (avg_y < ((1 / 3) * img.Cols))
                {
                    //they need to move right;
                    img.Draw("Right", new Point(0, img.Rows), Emgu.CV.CvEnum.FontFace.HersheyPlain, 4.0, new Bgra(0, 0, 255, 255), 3);
                }
            }
        }
        static void Main(string[] args)
        {
            // stream = new FileStream("C:\\video.264", FileMode.Create);
            //writ = new BinaryWriter(stream);
            initFrameConverter(width, height);
            models_names = new string[4];
            models_names[0] = @"intel_cpu";
            models_names[1] = @"asus_mobo";
            models_names[2] = @"nvidia_gpu";
            models_names[3] = @"cpu_fan";

            ServerController server = new ServerController();
            //for every delegate you want to function
            server.registerDelegate(CAPIStreamCommon.PacketType.VIDEO_FRAME, new ImageWork(processFrame));
            server.registerDelegate(CAPIStreamCommon.PacketType.GESTURE_DOWN, new ImageWork(handleGesture));
            offload_client.registerDelegate(CAPIStreamCommon.PacketType.REGISTER_TASK_REPLY, new PacketProcess(configureOffload));
            offload_client = new ConnectionControllerClient(ConnectionType.TCP, "192.168.82.9", 2275);

            /* register for task with offload server */
            if (offload_client.isConfigured())
            {
                String s = "intel_demo_task:raw:tcp";
                //send register task
                offload_client.sendDataPacket(new SocketData(PacketType.REGISTER_TASK, 1280, 720, 0, Encoding.ASCII.GetBytes(s)));
                offload_client.recieveDataPacketAsync();
            }

            //Begin accepting connections
            server.startServer(ConnectionType.TCP);
        }
        static void localPacketDispatch(SocketData d)
        {
            switch (d.message_type)
            {
                case PacketType.REGISTER_TASK_REPLY:
                    {
                        configureOffload(d);
                        break;
                    }
            }
        }
    }
}
