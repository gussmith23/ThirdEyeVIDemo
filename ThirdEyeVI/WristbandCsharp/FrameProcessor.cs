using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{
    class FrameProcessor: IFrameProcessor
    {
        private List<string> itemsAvailableForLocation;
        Tracker tracker = null;
        CMTTracker cmtTracker = null;
        SURFEngine testEngine = null;
        public event EventHandler<FrameProcessedEventArgs> FrameProcessed;

        protected virtual void OnFrameProcessed(FrameProcessedEventArgs e)
        {
            if (FrameProcessed != null)
            {
                FrameProcessed(this, e);
            }
        }

        public FrameProcessor(int trackType, Emgu.CV.Image<Emgu.CV.Structure.Bgr, Byte> itemImage)
        {
            switch (trackType)
            {
                // CMT + SURF
                case 0:
                    tracker = new CMTTracker(itemImage);
                    break;
                case 1:
                    throw new NotImplementedException("Write the code for Pure SURF tracking!");
                default:
                    tracker = null;
                    break;
            }
        }
        public FrameProcessor(int TrackType, string filename)
        {
            FrameProcessor(TrackType, new Emgu.CV.Image<Emgu.CV.Structure.Bgr, Byte>(filename));
        }

        public int Start()
        {
            return 0;
        }
        public int Pause()
        {
            return 0;
        }
        public int Stop()
        {
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
}
