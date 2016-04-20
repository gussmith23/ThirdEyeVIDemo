using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{
    class SURFEngine
    {

        SURFDetector surfDetector;
        Image<Gray, Byte> itemImage;
        VectorOfKeyPoint itemKP;
        Matrix<float> itemDescriptors;
        BruteForceMatcher<float> matcher;

        VectorOfKeyPoint observedKP;
        Matrix<float> observedDescriptors;
        Matrix<int> indices;
        Matrix<float> dist;
        Matrix<byte> mask;
        HomographyMatrix homography = null;
        PointF[] keyPts;
        int[] classes;
        const float errThreshold = 0.5f;
        PointF[] keypoints_tracked;
        int[] keypoint_classes;

        PointF[] projectedPoints;
        Rectangle roi;

        Image<Bgr, Byte> prevFrame;
        Image<Gray, Byte> curFrame;

        /// <summary>
        /// Used in the two-step SURF initialization - we find an initial ROI on the first call
        /// and return Rect.empty. Then, the next time around, we find another ROI and compare
        /// the two. If they're roughly the same, we return the ROI.
        /// </summary>
        Rectangle lastROI = Rectangle.Empty;

        private const double recall_threshold = 0.80d;

        public SURFEngine(Emgu.CV.Image<Bgr, byte> roi) : this(roi.Convert<Gray, byte>())
        {
        }

        public SURFEngine(Emgu.CV.Image<Gray, byte> roi)
        {
            surfDetector = new SURFDetector(500, false);
            itemImage = roi;

            itemKP = surfDetector.DetectKeyPointsRaw(itemImage, null);
            itemDescriptors = surfDetector.ComputeDescriptorsRaw(itemImage, null, itemKP);

            matcher = new BruteForceMatcher<float>(DistanceType.L2);
            matcher.Add(itemDescriptors);
        }

        /// <summary>
        /// Attempts to find a match in a frame.
        /// NOTE: This is a multi-step process! We get an initial ROI guess in the first call,
        /// and in the second call we compute a second guess. We then calculate recall 
        /// (intersection of ROIs over union of ROIs) and see if it passes some threshold.
        /// If it does, we return.
        /// Thus, this will have to be called at least twice to get a valid output ROI.
        /// </summary>
        /// <param name="image"></param>
        /// <returns>The ROI of the object, or Rectangle.Empty if nothing found.</returns>
        public Rectangle ProcessFrame(Image<Bgr, Byte> image)
        {
            // Invalidate old ROI/projected points
            roi = Rectangle.Empty;
            projectedPoints = null;

            Rectangle return_roi = Rectangle.Empty;

            Image<Gray, Byte> imageToDetect = image.Convert<Gray, Byte>();
            
            // Detect KP and calculate descriptors...
            observedKP = surfDetector.DetectKeyPointsRaw(image.Convert<Gray, Byte>(), null);
            observedDescriptors = surfDetector.ComputeDescriptorsRaw(image.Convert<Gray, Byte>(), null, observedKP);

            // Matching
            int k = 2;
            indices = new Matrix<int>(observedDescriptors.Rows, k);
            dist = new Matrix<float>(observedDescriptors.Rows, k);
            matcher.KnnMatch(observedDescriptors, indices, dist, k, null);

            //
            mask = new Matrix<byte>(dist.Rows, 1);
            mask.SetValue(255);
            Features2DToolbox.VoteForUniqueness(dist, 0.8, mask);

            int nonZeroCount = CvInvoke.cvCountNonZero(mask);
            if (nonZeroCount >= 4)
            {
                nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(itemKP, observedKP, indices, mask, 1.5, 20);

                // If we have enough info (???), create a homography matrix.
                if (nonZeroCount >= 4)
                    homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(itemKP, observedKP, indices, mask, 2); //last arg - 2 or 3?
                else homography = null;
            }


            // Get keypoints.
            keyPts = new PointF[itemKP.Size];
            classes = new int[itemKP.Size];
            for (int i = 0; i < itemKP.Size; i++)
            {
                keyPts[i] = itemKP[i].Point;
                classes[i] = itemKP[i].ClassId;
            }

            prevFrame = image;

            if (homography != null)
            {
                Rectangle rect = itemImage.ROI;
                projectedPoints = new PointF[] { 
                   new PointF(rect.Left, rect.Bottom),
                   new PointF(rect.Right, rect.Bottom),
                   new PointF(rect.Right, rect.Top),
                   new PointF(rect.Left, rect.Top)};
                homography.ProjectPoints(projectedPoints);
                
                // Get the ROI.
                PointF minXY = new PointF();
                minXY.X = float.MaxValue;
                minXY.Y = float.MaxValue;
                PointF maxXY = new PointF();
                maxXY.X = 0; maxXY.Y = 0;
                for (int i = 0; i < 4; i++)
                {
                    PointF pt = projectedPoints[i];
                    if (pt.X < minXY.X) minXY.X = pt.X;
                    if (pt.Y < minXY.Y) minXY.Y = pt.Y;
                    if (pt.X > maxXY.X) maxXY.X = pt.X;
                    if (pt.Y > maxXY.Y) maxXY.Y = pt.Y;
                    
                    roi = new Rectangle(
                    (int)minXY.X,
                    (int)minXY.Y,
                    (int)(maxXY.X - minXY.X),
                    (int)(maxXY.Y - minXY.Y));
                }
            }

            // Debug
            //Image<Bgr, Byte> result = Features2DToolbox.DrawMatches<Gray>(itemImage, itemKP, imageToDetect, observedKP, indices, new Bgr(255, 0, 0), new Bgr(255,0,0), mask, Features2DToolbox.KeypointDrawType.DRAW_RICH_KEYPOINTS);
            //result.DrawPolyline(Array.ConvertAll<PointF, Point>(projectedPoints, Point.Round), true, new Bgr(Color.Red), 5);
            //CvInvoke.cvShowImage("test", result);

            #region check against last ROI

            if (lastROI != Rectangle.Empty && roi != Rectangle.Empty)
            {
                Rectangle intersect = Rectangle.Intersect(lastROI, roi);
                int intersect_area = intersect.Width * intersect.Height;
                Rectangle union = Rectangle.Union(lastROI, roi);
                int union_area = union.Width * intersect.Height;

                double recall = (double)intersect_area / (double)union_area;

                // If it passes threshold...
                if (!double.IsNaN(recall) && !double.IsInfinity(recall) && recall > recall_threshold)
                {
                    return_roi = roi;
                }
                else
                {
                    return_roi = Rectangle.Empty;
                }
            }
            else
            {
                return_roi = Rectangle.Empty;
            }

            #endregion


            lastROI = roi;
            return return_roi;
        }

    }
}
