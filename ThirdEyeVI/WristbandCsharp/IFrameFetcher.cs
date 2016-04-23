using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{

    public delegate void FrameFetchedEventHandler(Object sender, FrameFetchedEventArgs e);

    interface IFrameFetcher
    { 
        //Raise event FrameFetched
        event FrameFetchedEventHandler FrameFetched;

        int Start();
        int Pause();
        int Stop();
    }
}
