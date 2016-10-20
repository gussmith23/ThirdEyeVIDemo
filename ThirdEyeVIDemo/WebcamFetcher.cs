using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV.UI;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO.Ports;
using System.IO;
using System.Threading;
using CAPIStreamServer;
using System.Runtime.InteropServices;

namespace ThirdEyeVIDemo
{
    public class WebcamFetcher : IFrameFetcher
    {
        int camera;
        Capture cap;
        Image<Bgr, Byte> image;
        bool stop = false;
        public event EventHandler<FrameFetchedEventArgs> FrameFetched;
        FrameFetchedEventArgs args;
        Thread thread;

        protected virtual void OnFrameFetched(FrameFetchedEventArgs e)
        {
            if (FrameFetched != null)
            {
                FrameFetched(this, e);
            }
        }

        public WebcamFetcher(float width, float height)
        {
            args = new FrameFetchedEventArgs();
            camera = 0;
            cap = new Capture(camera);
            width = 648.0f;
            height = 1152.0f;
            cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, height);
            cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, width);
        }
        public int Start()// Implement Framefetching. 
        {
            thread = new Thread(delegate ()
            {
                while (!stop)
                {
                    image = null;
                    while (image == null) image = cap.QueryFrame();
                    args.Frame = image;
                    OnFrameFetched(args);
                }
            });
            thread.Start();
            return 0;
        }
        public int Pause()
        {
            //thread.Suspend();
            return 0;
        }
        public int Stop()
        {
            stop = true;
            return 0;
        }
    }
}
