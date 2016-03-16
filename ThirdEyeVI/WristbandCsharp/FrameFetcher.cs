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
        int start();
        int pause();
        int stop();
    }
}
