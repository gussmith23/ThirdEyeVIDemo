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
    interface IFrameProcessor
    {
        void FrameFetched(object sender, FrameFetchedEventArgs e);
        
    }
}
