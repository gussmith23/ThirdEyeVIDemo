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
    interface FrameProcessor
    {
        event EventHandler RawFrame;

        event EventHandler FrameProcessed;
        
        //eg of class would be CMT tracker
        int Start();
        int Pause();
        int Stop();
    }
}
