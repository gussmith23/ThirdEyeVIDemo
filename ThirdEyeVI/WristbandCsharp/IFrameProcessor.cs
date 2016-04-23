using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
 * This inteface will send tracking data and receive
 * frame received event + frame data
 */

namespace WristbandCsharp
{
    public delegate void FrameProcessedEventHandler(Object sender, FrameProcessedEventArgs e);

    interface IFrameProcessor
    {
        //Raise event FrameProcessed
        event FrameProcessedEventHandler FrameProcessed;

        int Start();
        int Pause();
        int Stop();

    }
}
