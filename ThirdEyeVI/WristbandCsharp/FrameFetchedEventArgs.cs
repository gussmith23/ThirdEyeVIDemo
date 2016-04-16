using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.UI;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace WristbandCsharp
{
    class FrameFetchedEventArgs: EventArgs
    {
        
        public Image<Bgr,byte> Frame { get; set; }

      

    }
}
