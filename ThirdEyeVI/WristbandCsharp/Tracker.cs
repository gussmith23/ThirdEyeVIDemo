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
        public Point CenterOfObject;

        /// <summary>
        /// This method should not return anything; it should instead update the Tracker object's ROI.
        /// The return type is a matter of legacy.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public abstract Image<Bgr,Byte> Process(Image<Bgr, Byte> image);


    }
}
