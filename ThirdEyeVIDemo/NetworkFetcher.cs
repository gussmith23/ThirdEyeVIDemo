using CAPIStreamServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAPIStreamCommon;
using Emgu.CV;
using Emgu.CV.Structure;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV.UI;
using Emgu.Util;
using System.IO.Ports;
using System.IO;
using System.Threading;
using ObjectSpeechRecognizer;
using System.Runtime.InteropServices;
using static ThirdEyeVIDemo.NetworkFetcher;

namespace ThirdEyeVIDemo
{
    

    public class NetworkFetcher : IFrameFetcher  //implementing interface
    {    

        
        public event EventHandler<FrameFetchedEventArgs> FrameFetched;       

        protected virtual void OnFrameFetched(FrameFetchedEventArgs e)
        {
            if(FrameFetched != null)
            {
                FrameFetched(this, e);
            }
        }


        #region DLL imports for Peter's color conversion functions.

        /**
         * TODO there are problems here on different Windows platforms.
         * I tried running this on Win10 and (using dependency walker) I discovered
         * that needed Windows API dlls couldn't be found by ffmpeg_export.dll.
         * Peter compiled this DLL (I think), and I think he did it on Win7 or 8.
         * He may need to recompile, and a solution needs to be found to avoid this 
         * in the future.
         */

        [DllImport(@"ffmpeg_export.dll")]
        private static extern IntPtr convertYUVtoRGB(IntPtr yuv_data, int width, int height);

        [DllImport(@"ffmpeg_export.dll")]
        private static extern void initFrameConverter(int width, int height);

        [DllImport(@"ffmpeg_export.dll")]
        private static extern void deinitFrameConverter();

        #endregion

        // Streaming.
        // Expected width and height.
        int stream_width = 640, stream_height = 480;

        ServerController server; //This creates a server object

        

        //object objectLock = new object();

       /* event EventHandler IFrameFetcher.FrameFetched
        {
            add
            {
                lock (objectLock)
                {
                    FrameFetched += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    FrameFetched -= value;
                }
            }
        }
        */

        NetworkFetcher()//we don't want it to start until we tell it to start,
                        //therefore, start() is outside the constructor
        {
            server = new ServerController();//Here the server object is initialized
        }

        public int Start()
        {
           server.registerDelegate(CAPIStreamCommon.PacketType.VIDEO_FRAME, new ImageWork(FetchImageFromNetwork));
            return 0;
        }

        private SocketData FetchImageFromNetwork(SocketData d /*frame is passed*/) //converts frame into usable frame
        {


            #region declarations
            Image<Bgr, Byte> return_image;
            #endregion

            #region Convert to usable image + place in return_image using Peter's DLLs.

            // Contact Peter Zientara about this piece of code.

            if (d == null ) return null;

            int size = stream_width * stream_height * 3;
            byte[] rgb_data = new byte[size];
            unsafe
            {
                IntPtr byteArray = Marshal.AllocHGlobal(d.data.Length);
                Marshal.Copy(d.data, 0, byteArray, d.data.Length);
                IntPtr rgb_data_ptr;
                rgb_data_ptr = convertYUVtoRGB(byteArray, stream_width, stream_height);
                Marshal.FreeHGlobal(byteArray);
                Marshal.Copy(rgb_data_ptr, rgb_data, 0, size);
            }
            Image<Bgr, Byte> converted_image = new Image<Bgr, Byte>(stream_width, stream_height);
            Buffer.BlockCopy(rgb_data, 0, converted_image.Data, 0, size);
            //CvInvoke.cvShowImage("frame", image);
            //CvInvoke.cvWaitKey(1);

            return_image = converted_image;

            #endregion

            FrameFetchedEventArgs argsNetwork = new FrameFetchedEventArgs();

            argsNetwork.Frame = return_image;

            FrameFetched?.Invoke(this, argsNetwork);

            return null;

            
        }

        public int Pause()
        {
            throw new NotImplementedException();
        }

        

        public int Stop()
        {
            throw new NotImplementedException();
        }
    }
}
