﻿using System;
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
        //Raise event FrameProcessed
        event EventHandler<FrameProcessedEventArgs> FrameProcessed;

        int Start();
        int Pause();
        int Stop();

    }
}
