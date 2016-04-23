using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace WristbandCsharp
{
    class FrameProcessedEventArgs
    {
        public Image<Bgr, byte> Frame { get; set; }
    }
}
