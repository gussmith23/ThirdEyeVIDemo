﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
 * This interface will send frame received event+frame data
 */

namespace WristbandCsharp
{
    interface FrameFetcher
    {
        int Start();
        int Pause();
        int Stop();
        //eg. of classes implementing interface be Network Fetcher or Webcam Fetcher
    }

}
