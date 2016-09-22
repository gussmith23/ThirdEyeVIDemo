using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace WristbandCsharp
{
    public class FrameProcessedEventArgs
    {
        public Image<Bgr, byte> Frame { get; set; } //Frame Processed
        //ROI -> Rectangle
    }
}
