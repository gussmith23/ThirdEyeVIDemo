using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{


    interface IFrameFetcher
    {
        //Raise event FrameFetched
        event EventHandler<FrameFetchedEventArgs> FrameFetched; //Event with Args
        int Start();
        int Pause();
        int Stop();
    }
}
