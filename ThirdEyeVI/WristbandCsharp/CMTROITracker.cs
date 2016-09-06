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
        private Image<Bgr, Byte> ModelImage;
        private Size PatchSize = Size.Empty;
        private CMTTracker Tracker = null;

        /// <summary>
        /// CMTROITracker will use FindBestROIUtil to break the image up into chunks and find which chunk
        /// most matches the model image. Then, it will track that chunk. The idea here is that, if there 
        /// are many duplicates of the object represented by the model image (e.g. like on a grocery store
        /// shelf) then this method will reduce the issues caused by having duplicates.
        /// </summary>
        /// <param name="ModelImage">the "ideal" image of what we'd like to track.</param>
        public CMTROITracker(Image<Bgr, byte> ModelImage, Size PatchSize)
        {
            this.ModelImage = ModelImage;
            this.PatchSize = PatchSize;
        }


        public override Image<Bgr, Byte> Process(Image<Bgr, Byte> image)
        {
            if (Tracker == null)
            {
                Rectangle BestROI = FindBestROIUtil.FindBestROIUtil.FindBestROI(image.Convert<Gray, byte>(), ModelImage.Convert<Gray, byte>(), PatchSize);
                if (BestROI != Rectangle.Empty)
                {
                    Image<Bgr, byte> ROIImage = image.Clone();
                    ROIImage.ROI = BestROI;
                    Tracker = new CMTTracker(ROIImage);
                    return Tracker.Process(image);
                }
                return image;
            }
            else
            {
                return Tracker.Process(image);
            }
        }
    }
}
