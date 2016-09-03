using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.UI;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using CMT_Tracker;

namespace WristbandCsharp
{
    class CMTROITracker : Tracker
    {
        private bool Initialized = false;
        Image<Bgr, Byte> ModelImage;
        Size PatchSize = new Size(300, 300);
        CMTTracker tracker = null;

        /// <summary>
        /// CMTTracker will attempt to find an object in the frame using SURF and then track
        /// it using CMT. 
        /// </summary>
        /// <param name="roi">the "ideal" image of what we'd like to track.</param>
        public CMTROITracker(Emgu.CV.Image<Bgr, byte> roi)
        {
            ModelImage = roi;
        }


        public override Image<Bgr, Byte> Process(Image<Bgr, Byte> image)
        {
            if (tracker == null)
            {
                Rectangle BestROI = FindBestROIUtil.FindBestROIUtil.FindBestROI(image.Convert<Gray, byte>(), ModelImage.Convert<Gray, byte>(), PatchSize);
                if (BestROI != Rectangle.Empty)
                {
                    Image<Bgr, byte> ROIImage = image.Clone();
                    ROIImage.ROI = BestROI;
                    tracker = new CMTTracker(ROIImage);
                    return tracker.Process(image);
                }
                return image;
            }
            else
            {
                return tracker.Process(image);
            }
        }
    }
}
