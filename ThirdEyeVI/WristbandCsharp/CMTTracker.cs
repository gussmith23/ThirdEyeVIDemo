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
    class CMTTracker : Tracker
    {
        SURFEngine surfEngine;

        CMT cmtEngine;
        Image<Bgr, Byte> prevFrame;
        Image<Gray, Byte> curFrame;
        PointF[] keyPts;
        int[] classes;
        const float errThreshold = 0.5f;
        PointF[] keypoints_tracked;
        int[] keypoint_classes;
        public Rectangle roi;

        Image<Gray,Byte> itemImage;
        VectorOfKeyPoint itemKP;
        Matrix<float> itemDescriptors;
        VectorOfKeyPoint observedKP;
        Matrix<float> observedDescriptors;
        BruteForceMatcher<float> matcher;
        Matrix<int> indices;
        Matrix<float> dist;
        Matrix<byte> mask;
        HomographyMatrix homography = null;

        public PointF centerOfObject = PointF.Empty;

        Bgr color = new Bgr(0,255,255);
        int thickness = 5;

        /* 
         * This boolean specifies whether or not we'll use CMT to simplify tracking. 
         * When using CMT, we use SURF to find the object, and then pass it to CMT to be tracked.
         */
        [System.Obsolete("CMT vs pure SURF was split into two separate classes. This decision is no longer made here.")]
        public Boolean trackWithCMT = true;

        // Points projected by SURF
        PointF[] projectedPoints = null;

        SURFDetector surfDetector;

        public CMTTracker(string directory) : this(new Image<Bgr, byte>(directory))
        {
        }

        /// <summary>
        /// CMTTracker will attempt to find an object in the frame using SURF and then track
        /// it using CMT. 
        /// </summary>
        /// <param name="roi">the "ideal" image of what we'd like to track.</param>
        public CMTTracker(Emgu.CV.Image<Bgr, byte> roi)
        {
            
            // I HAD TO CHANGE CMT TO PUBLIC FOR THIS TO WORK.
            cmtEngine = new CMT_Tracker.CMT();
            surfEngine = new SURFEngine(roi);

        }

        [Obsolete]
        public Image<Bgr,Byte> process(Image<Bgr,Byte> image)
        {

            //Console.WriteLine(cmtTracker.Initialized);

            if (cmtEngine.Initialized == true && trackWithCMT)
            {
                try
                {
                    roi = cmtEngine.ProcessFrame(image, roi);
                    FindCenter();
                    return DrawCenterOfObjectOnImage(image);
                }
                catch (NullReferenceException e)
                {
                    return image;
                }
            }

            else SURFDetect(image);

            FindCenter();

            return DrawROIOnImage(image);

        }

        public override Image<Bgr,Byte> Process(Image<Bgr,Byte> image)
        {
            // First we update our ROI - either by another call to CMT, or by calling SURF (and then initializing CMT afterwards)
            if (cmtEngine.Initialized)
            {
                // This is pretty much good as it is; might wanna rename the object to cmtEngine.
                try
                { 
                    roi = cmtEngine.ProcessFrame(image,roi);
                } catch (Exception e)
                {
                    
                }
            }
            else
            {
                /*
                 * Note that we don't actually use the CMT results in this iteration - we just initialize it and 
                 * hope that it's initialized next time around. If not, SURF will just run again.
                 */

                // Detect with SURF...
                roi = surfEngine.ProcessFrame(image);

                // If SURF results are valid, initialize CMT with results.
                if(roi != Rectangle.Empty) cmtEngine.Initialize(image, roi);

            }

            centerOfObject.X = roi.X + roi.Width / 2;
            centerOfObject.Y = roi.Y + roi.Height / 2;

            return DrawROI(image);
        }

        private Image<Bgr,byte> DrawROI(Image<Bgr, byte> image)
        {

            // If we didn't detect anything, immediately return the image.
            if (roi == Rectangle.Empty) return image;
            
            // Draw ROI.
            image.Draw(roi, color, thickness);

            return image;
            
        }

        private void FindCenter()
        {
            if (roi == Rectangle.Empty) return;

            centerOfObject = new PointF(
                (roi.Left + roi.Right) / 2,
                (roi.Top + roi.Bottom) / 2
                );
        }


        [Obsolete]
        public void SURFDetect(Image<Bgr, Byte> image)
        {
            
            // Detect KP and calculate descriptors...
            observedKP = surfDetector.DetectKeyPointsRaw(image.Convert<Gray,Byte>(), null);
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
                if (nonZeroCount >= 4)
                    homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(itemKP, observedKP, indices, mask, 3);
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

            #region 

            // Find ROI
            PointF minXY = new PointF();
            PointF maxXY = new PointF();
            for (int i = 0; i < itemKP.Size; i++)
            {
                PointF pt = keyPts[i];
                if (pt.X < minXY.X) minXY.X = pt.X;
                if (pt.Y < minXY.Y) minXY.Y = pt.Y;
                if (pt.X > maxXY.X) maxXY.X = pt.X;
                if (pt.Y > maxXY.Y) maxXY.Y = pt.Y;
            }

            // Convert ROI to rect
            //roi = new Rectangle((int)minXY.X, (int)minXY.Y, (int)(maxXY.X - minXY.X), (int)(maxXY.Y - minXY.Y));

            //Console.WriteLine("Position: ({0},{1}) \tWidth: {2}\tHeight: {3}", roi.X, roi.Y, roi.Width, roi.Height);
            
            #endregion

            
            projectedPoints = null;
            if (homography != null) {
                Rectangle rect = itemImage.ROI;
                projectedPoints = new PointF[] { 
                   new PointF(rect.Left, rect.Bottom),
                   new PointF(rect.Right, rect.Bottom),
                   new PointF(rect.Right, rect.Top),
                   new PointF(rect.Left, rect.Top)};
                homography.ProjectPoints(projectedPoints);

                roi = new Rectangle((int)(projectedPoints[3].X), (int)projectedPoints[3].Y, (int)(projectedPoints[1].X - projectedPoints[3].X), (int)(projectedPoints[1].Y - projectedPoints[3].Y));
                
                // We're always gonna track with CMT now, so this will get initialized no matter what.
                /*
                 * if (trackWithCMT) cmtTracker.Initialize(image, roi);l
                 * else cmtTracker = null;
                 */

                // Initialize CMT unconditionally.
                cmtEngine.Initialize(image, roi);

            }


            

        }

        [Obsolete]
        Image<Bgr, Byte> DrawROIOnImage(Image<Bgr, Byte> image)
        {

            // TODO Note: I just kinda threw this code in here cause i don't need it. Figure out if it's still needed.
            if (trackWithCMT)
            {
                Point[] points = new Point[4];
                points[0] = new Point(roi.X, roi.Y);
                points[1] = new Point(roi.X + roi.Width, roi.Y);
                points[2] = new Point(roi.X, roi.Y + roi.Height);
                points[3] = new Point(roi.X + roi.Width, roi.Y + roi.Height);

                image.DrawPolyline(points, true, new Bgr(100, 0, 0), 5);
            }
            else
            {
                Point[] projectedPointsRounded = new Point[4];
                for (int i = 0; i < projectedPoints.Length; i++)
                {
                    projectedPointsRounded[i] = Point.Round(projectedPoints[i]);
                }

                image.DrawPolyline(projectedPointsRounded, true, new Bgr(100, 0, 0), 10);

                Console.WriteLine("Test!");
                
            }
            
            return image;
        }


        [Obsolete]
        Image<Bgr, Byte> DrawCenterOfObjectOnImage(Image<Bgr, Byte> image)
        {
            image.Draw(new CircleF(centerOfObject, 25), new Bgr(0,255,255), 0);

            return image;
        }

        // Find the direction to force the hand in. 0 =
        [Obsolete]      // not sure why this is marked as obsolete.
        public static int findDirection(PointF centerOfObject, PointF centerOfScreen)
        {
            // percent distancce from center considered when moving forward.
            int percentFromCenterTolerated = 15;

            float distance = (float)Math.Sqrt(
                Math.Pow(centerOfObject.X - centerOfScreen.X, 2) +
                Math.Pow(centerOfObject.Y - centerOfScreen.Y, 2));

            int percentFromCenter = (int) (100 * (distance / centerOfScreen.X));

            if (percentFromCenter < percentFromCenterTolerated) return 5;


            // Vector pointing from center of screen to the object.
            // Notice we negate the Y term - this is to put theta in terms of the Cartesian plane we generally think in,
            //      not int terms of winforms' coordinates (where down is positive Y)
            PointF pointingVector = new PointF(
                centerOfScreen.X - centerOfObject.X,
                 -(centerOfScreen.Y - centerOfObject.Y)
                );

            

            double theta = (Math.Atan2(pointingVector.Y, pointingVector.X)); //+ 45.0*(Math.PI / 180.0)) % (2.0 * Math.PI);

            double thetaPercent = (theta / (2.0 * Math.PI)) * 100.0;

            
            if (thetaPercent > -25.0/2.0 && thetaPercent <= 25.0/2.0) return 0;
            else if (thetaPercent > 25.0/2.0 && thetaPercent <= 25.0 + 25.0/2.0) return 1;
            else if (thetaPercent <= -25.0 + -25.0 / 2.0 || thetaPercent > 25.0 + 25.0 / 2.0) return 2;
            else if (thetaPercent > -25 + -25.0 / 2.0 && thetaPercent <= -25.0 / 2.0) return 3;
            else return -1;
         
        }
        
    }
}
