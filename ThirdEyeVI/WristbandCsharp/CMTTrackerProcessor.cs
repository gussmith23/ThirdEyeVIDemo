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
using Speech;
using System.Threading;
using ObjectSpeechRecognizer;
using System.Runtime.InteropServices;



namespace WristbandCsharp
{
    class CMTTrackerProcessor : IFrameProcessor
    {

        public event FrameProcessedEventHandler FrameProcessed;
        private Image<Bgr, Byte> frame_fetched;
        public IFrameFetcher fetcher { get; set; }        

        //Constructor
        public CMTTrackerProcessor()
        {

        }

        public CMTTrackerProcessor(IFrameFetcher fetcher)
        {
            this.fetcher = fetcher;
        }

        private void FrameFetched(object sender, FrameFetchedEventArgs e)
        {
            // Get frame
            frame_fetched = e.Frame;

            // DO IMAGE PROCESSING STUFF

            // GET RESULTS

            // Make FrameProcessedEventArgs
            FrameProcessedEventArgs ProcessedArgs = new FrameProcessedEventArgs();

            // Call FrameProcessed event.
            FrameProcessed(this, ProcessedArgs);

        }        

        //protected virtual void OnFrameProcessed(FrameProcessedEventArgs e)
        //{
        //    if (FrameProcessed != null)
        //    {
        //        FrameProcessed(this, e);
        //    }
        //}
        
        public int Start()
        {
            if (fetcher != null)
            {
                fetcher.FrameFetched += new FrameFetchedEventHandler(FrameFetched);
            }
            return 0;
        }

       

        public int Pause()
        {
            if (fetcher != null)
            {
                fetcher.FrameFetched -= new FrameFetchedEventHandler(FrameFetched);
            }
            return 0;
        }
        
        public int Stop()
        {
            throw new NotImplementedException();
        }
    }
}
