using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAPIStreamServer;

namespace WristbandCsharp
{
    interface IFrameFetcher
    { 
        //Raise event FrameFetched
        event EventHandler FrameFetched;

        int Start();
        int Pause();
        int Stop();
    }
}
