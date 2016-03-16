using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{
    interface FrameFetcher
    {
        int Init();
        int Start();
        int Pause();
        int Stop();
    }
}
