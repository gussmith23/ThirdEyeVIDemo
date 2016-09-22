using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{
    class FrameProcessor: IFrameProcessor
    {
        public event EventHandler<FrameProcessedEventArgs> FrameProcessed;

        protected virtual void OnFrameProcessed(FrameProcessedEventArgs e)
        {
            if (FrameProcessed != null)
            {
                FrameProcessed(this, e);
            }
        }

        FrameProcessor(float width, float height)
        {
        }
        public int Start()// Implement Framefetching. 
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
    }
}
}
