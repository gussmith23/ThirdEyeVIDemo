using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ThirdEyeVIDemo
{
    public class FrameProcessor: IFrameProcessor
    {
        private List<string> itemsAvailableForLocation;
        Tracker tracker = null;
        CMTTracker cmtTracker = null;
        SURFEngine testEngine = null;
        public event EventHandler<FrameProcessedEventArgs> FrameProcessed;
        FrameProcessedEventArgs args;
        int trackType;
        string filename;
        Emgu.CV.Image<Emgu.CV.Structure.Bgr, Byte> itemImage;
        Thread thread;

        protected virtual void OnFrameProcessed(FrameProcessedEventArgs e)
        {
            if (FrameProcessed != null)
            {
                FrameProcessed(this, e);
            }
        }

        public FrameProcessor(int trackType , Emgu.CV.Image<Emgu.CV.Structure.Bgr, Byte> itemImage)
        {
            this.trackType = trackType;
            this.itemImage = itemImage;

        }

        public FrameProcessor(int trackType, string filename)
        {
            this.trackType = trackType;
            this.filename = filename;

        }

        public int Start()
        {
            thread = new Thread(delegate ()
            {
                switch (trackType)
                {
                    // CMT + SURF
                    case 0:
                        if (filename != null) tracker = new CMTTracker(new Emgu.CV.Image<Emgu.CV.Structure.Bgr, Byte>(filename));
                        else tracker = new CMTTracker(itemImage);
                        args.roi = tracker.roi;
                        OnFrameProcessed(args);
                        break;
                    case 1:
                        throw new NotImplementedException("Write the code for Pure SURF tracking!");
                    default:
                        tracker = null;
                        break;
                }
            });
            thread.Start();
            return (0);
        }
        public int Pause()
        {
            thread.Suspend();
            return 0;
        }
        public int Stop()
        {
            thread.Abort();
            return 0;
        }

        List<string> itemsAvailableTrack()
        {
            itemsAvailableForLocation = new List<string>();
            string[] itemNames = System.IO.Directory.GetFiles("itemsToTrack/", "*.jpg");
            foreach (string s in itemNames)
            {
                string name = System.Text.RegularExpressions.Regex.Replace(s, "itemsToTrack/", "");
                name = System.Text.RegularExpressions.Regex.Replace(name, ".jpg", "");
                itemsAvailableForLocation.Add(name);
            }
            return itemsAvailableForLocation;
        }
    }
}
