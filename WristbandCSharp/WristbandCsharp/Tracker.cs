using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{
    public abstract class Tracker
    {

        public Rectangle roi;

        /**
         * This method should not return anything; it should instead update the Tracker object's ROI.
         */
        public abstract Image<Bgr,Byte> Process(Image<Bgr, Byte> image);


    }
}
