using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CMT_Tracker
{
    /// <summary>
    /// C# Implementation of Consensus-based Matching and Tracking of Keypoints.
    /// <see cref="http://www.gnebehay.com/cmt/"/>
    /// <see cref="Nebehay, Georg and Pflugfelder, Roman. Consensus-based Matching and Tracking of Keypoints for Object Tracking. Winter Conference on Applications of Computer Vision 2014."/>
    /// </summary>
    public class CMT
    {
        #region Private Members
        //private SURFDetector mFeatureDetector;
        private Brisk mFeatureDetector;
        private BruteForceMatcher<byte> mObjectMatcher;
        private BruteForceMatcher<byte> mGlobalMatcher;

        private PointF mCenterTopLeft;
        private PointF mCenterTopRight;
        private PointF mCenterBottomRight;
        private PointF mCenterBottomLeft;
        #endregion

        #region Private Members of Public Properties
        private float mOpticalFlowErrorThreshold = 20.0F;
        private int mClusterOutlierThreshold = 20;
        private int mMatchingConfidenceScaleFactor = 512;
        private float mMatchingConfidenceThreshold = 0.7F;
        private float mMatchingConfidenceRatio = 0.8F;

        private bool bEstimateScale;
        private bool bEstimateRotation;
        private bool bInitialized;
        private bool bValid;

        private float mRotationEstimate;
        private float mScaleEstimate;

        private RectangleF mBoundingBox;
        private PointF mCenter;

        private List<PointF> mAllKeypoints;
        private Matrix<byte> mAllKeypointFeatures;
        private List<int> mAllKeypointClasses;
        private int mInitialKeypointCount;

        private List<PointF> mActiveKeypoints;
        private Matrix<byte> mActiveKeypointFeatures;
        private List<int> mActiveKeypointClasses;

        private List<PointF> mSelectedKeypoints;
        private Matrix<byte> mSelectedKeypointFeatures;
        private List<int> mSelectedKeypointClasses;
        private Matrix<float> mSelectedKeypointDistances;
        private Matrix<float> mSelectedKeypointAngles;

        private List<PointF> mSprings;

        private List<PointF> mTrackedKeypoints;
        private List<PointF> mInliers;
        private List<PointF> mOutliers;

        private Image<Gray, Byte> mPreviousFrame;
        #endregion

        #region Public Properties
        /// <summary>
        /// Scaling factor to be used in computing matching confidence metric.
        /// </summary>
        public int MatchingConfidenceScaleFactor { get { return mMatchingConfidenceScaleFactor; } set { mMatchingConfidenceScaleFactor = value; } }

        /// <summary>
        /// Minimum distance between cluster centroids during center-estimate clustering.
        /// </summary>
        public int ClusterOutlierThreshold { get { return mClusterOutlierThreshold; } set { mClusterOutlierThreshold = value; } }

        /// <summary>
        /// Minimum confidence required to be considered a sufficiently good match.
        /// </summary>
        public float MatchingConfidenceThreshold { get { return mMatchingConfidenceThreshold; } set { mMatchingConfidenceThreshold = value; } }
        
        /// <summary>
        /// Minimum confidence ratio required between top two best matches to be considered a sufficiently good match.
        /// </summary>
        public float MatchingConfidenceRatio { get { return mMatchingConfidenceRatio; } set { mMatchingConfidenceRatio = value; } }

        /// <summary>
        /// Maximum error threshold allowed in reverse-computed optical flow to be considered a stable tracked point.
        /// </summary>
        public float OpticalFlowErrorThreshold { get { return mOpticalFlowErrorThreshold; } set { mOpticalFlowErrorThreshold = value; } }

        /// <summary>
        /// Boolean toggle indicating whether the tracker should estimate the scale of the object being tracked.
        /// </summary>
        public bool EstimateScale { get { return bEstimateScale; } set { bEstimateScale = value; } }

        /// <summary>
        /// Boolean toggle indicating whether the tracker should estimate the rotation of the object being tracked.
        /// </summary>
        public bool EstimateRotation { get { return bEstimateRotation; } set { bEstimateRotation = value; } }

        /// <summary>
        /// Boolean flag indicating whether the tracker has been initialized with a model and ROI
        /// </summary>
        public bool Initialized { get { return bInitialized; } }

        /// <summary>
        /// Boolean flag indicating whether the tracker currently has a valid tracking result.
        /// </summary>
        public bool Valid { get { return bValid; } }

        /// <summary>
        /// The current estimate of the rotation of the object, relative to the +X axis, in radians.
        /// </summary>
        public float RotationEstimate { get { return mRotationEstimate; } }

        /// <summary>
        /// The current estimate of the scale of the object, relative to its original size (1.0)
        /// </summary>
        public float ScaleEstimate { get { return mScaleEstimate; } }

        /// <summary>
        /// Rectangle describing the current object bounding box, represented in floating-point coordinates. Only valid if <i>this.Valid</i> is true.
        /// </summary>
        public RectangleF BoundingBoxF { get { return new RectangleF(mBoundingBox.Location, mBoundingBox.Size); } }
        /// <summary>
        /// Rectangle describing the current object bounding box, represented in integer coordinates. Only valid if <i>this.Valid</i> is true.
        /// </summary>
        public Rectangle BoundingBox { get { return RectFToRect(mBoundingBox); } }

        /// <summary>
        /// Point describing the current estimate of the center of the object, represented in floating-point coordinates.  Only valid if <i>this.Valid</i> is true.
        /// </summary>
        public PointF CenterF { get { return mCenter; } }
        /// <summary>
        /// Point describing the current estimate of the center of the object, represented in integer coordinates.  Only valid if <i>this.Valid</i> is true.
        /// </summary>
        public Point Center { get { return PointFToPoint(mCenter); } }

        /// <summary>
        /// Returns a list of coordinates of the currently active keypoints, represented in floating-point coordinates.
        /// </summary>
        public List<PointF> ActiveKeypointsF { get { return mActiveKeypoints; } }
        /// <summary>
        /// Returns a single coordinate, from the list currently active keypoints, represented in integer coordinates.
        /// </summary>
        /// <param name="i">The index of the active coordinate to be returned if the index is in range (0 &lt; i &lt;= this.ActiveKeypointsF.Count; this.ActiveKeypointsF.Count &gt; 0). If the index is out of range, the point (0,0) is returned.</param>
        /// <returns>The Point coordinate at the selected index, if the index is valid, Point(0,0) otherwise.</returns>
        public Point ActiveKeypoints(int i) { return (i < 0 ? new Point() : (i < mActiveKeypoints.Count ? new Point((int)mActiveKeypoints[i].X, (int)mActiveKeypoints[i].Y) : new Point())); }

        /// <summary>
        /// Returns a list of coordinates of the currently tracked keypoints, represented in floating-point coordinates.
        /// </summary>
        public List<PointF> TrackedKeypoints { get { return (mTrackedKeypoints == null ? new List<PointF>() : mTrackedKeypoints); } }

        /// <summary>
        /// Returns a list of coordinates of the currently clustered inlier keypoints, represented in floating-point coordinates.
        /// </summary>
        public List<PointF> Inliers { get { return (mInliers == null ? new List<PointF>() : mInliers); } }

        /// <summary>
        /// Returns a list of coordinates of the currently clustered outlier keypoints, represented in floating-point coordinates.
        /// </summary>
        public List<PointF> Outliers { get { return (mOutliers == null ? new List<PointF>() : mOutliers); } }

        #endregion

        #region -- Optical Flow -- Delegates
        /// <summary>
        /// Delegate function defining the interface to an external Optical Flow Computation routine.
        /// </summary>
        /// <param name="prevFrame">The grayscale image of the previous frame.</param>
        /// <param name="curFrame">The grayscale image of the current frame.</param>
        /// <param name="pts">The points to be tracked.</param>
        /// <param name="numPts">The number of points to be tracked.</param>
        /// <param name="newPts">The new locations of the keypoints, computed from forward optical flow.</param>
        /// <param name="bckPts">The locations of the recalculated current points, returned for post-optical flow error computation.</param>
        public delegate void ComputeOpticalFlowDelegate(Image<Gray, Byte> prevFrame, Image<Gray, Byte> curFrame, PointF[] keyPts, int NumKeypoints, out PointF[] newPts, out PointF[] bckPts);

        /// <summary>
        /// Delegate function external hosts may use to replace default Lucas-Kanade optical flow.
        /// </summary>
        public ComputeOpticalFlowDelegate OpticalFlowFunction;

        /// <summary>
        /// Wrapper function for computing optical flow. If <i>this.OpticalFlowFunction != null</i>, it is called. Otherwise, the default Lucas-Kanade Optical Flow is invoked.
        /// </summary>
        /// <param name="prevFrame">The grayscale image of the previous frame.</param>
        /// <param name="curFrame">The grayscale image of the current frame.</param>
        /// <param name="pts">The points to be tracked.</param>
        /// <param name="numPts">The number of points to be tracked.</param>
        /// <param name="newPts">The new locations of the keypoints, computed from forward optical flow.</param>
        /// <param name="bckPts">The locations of the recalculated current points, returned for post-optical flow error computation.</param>
        private void ComputeOpticalFlow(Image<Gray, Byte> prevFrame, Image<Gray, Byte> curFrame, PointF[] pts, int numPts, out PointF[] newPts, out PointF[] bckPts)
        {
            if (OpticalFlowFunction == null)
            {
                ComputeLKOpticalFlow(prevFrame, curFrame, pts, numPts, out newPts, out bckPts);
            }
            else
            {
                OpticalFlowFunction(prevFrame, curFrame, pts, numPts, out newPts, out bckPts);
            }
        }

        /// <summary>
        /// Computes both forward and reverse Lucas Kanade Pyramid Optical Flow.
        /// </summary>
        /// <param name="prevFrame">The grayscale image of the previous frame.</param>
        /// <param name="curFrame">The grayscale image of the current frame.</param>
        /// <param name="pts">The points to be tracked.</param>
        /// <param name="numPts">The number of points to be tracked.</param>
        /// <param name="newPts">The new locations of the keypoints, computed from forward optical flow.</param>
        /// <param name="bckPts">The locations of the recalculated current points, returned for post-optical flow error computation.</param>
        private static void ComputeLKOpticalFlow(Image<Gray, Byte> prevFrame, Image<Gray, Byte> curFrame, PointF[] pts, int numPts, out PointF[] newPts, out PointF[] bckPts)
        {
            Console.WriteLine("Internal Lucas-Kanade Optical Flow");
            Size opticalFlowWindow = new Size(21, 21);
            MCvTermCriteria opticalFlowCriteria = new MCvTermCriteria(10, 0.03);
            opticalFlowCriteria.type = Emgu.CV.CvEnum.TERMCRIT.CV_TERMCRIT_EPS | Emgu.CV.CvEnum.TERMCRIT.CV_TERMCRIT_ITER;
            int opticalFlowLevel = 2;

            byte[] fwdStatus;
            float[] fwdError;
            byte[] bckStatus;
            float[] bckError;

            OpticalFlow.PyrLK(prevFrame, curFrame,
                pts, opticalFlowWindow,
                opticalFlowLevel, opticalFlowCriteria,
                out newPts, out fwdStatus, out fwdError);

            OpticalFlow.PyrLK(curFrame, prevFrame,
                newPts, opticalFlowWindow,
                opticalFlowLevel, opticalFlowCriteria,
                out bckPts, out bckStatus, out bckError);
        }
        
        /// <summary>
        /// Computes optical flow error, given the original points and reverse-computed points.
        /// </summary>
        /// <param name="pts">The points to be tracked.</param>
        /// <param name="numPts">The number of points to be tracked.</param>
        /// <param name="bckPts">The locations of the recalculated current points, returned for post-optical flow error computation.</param>
        /// <param name="errX">The optical flow error in the X-direction.</param>
        /// <param name="errY">The optical flow error in the Y-direction.</param>
        private static void ComputeOpticalFlowError(PointF[] pts, int numPts, PointF[] bckPts, out float errX, out float errY)
        {
            errX = 0;
            errY = 0;
            for (int i = 0; i < numPts; i++)
            {
                errX += (float)Math.Sqrt(Math.Pow(bckPts[i].X - pts[i].X, 2));
                errY += (float)Math.Sqrt(Math.Pow(bckPts[i].Y - pts[i].Y, 2));
            }
        }
        #endregion

        #region -- Keypoint Detection -- Delegates
        /// <summary>
        /// Delegate function defining the interface to an external Keypoint Extraction routine.
        /// </summary>
        /// <param name="frame">The grayscale image of the current frame.</param>
        /// <param name="all_keypoints">The keypoints extracted from the given frame.</param>
        public delegate void ExtractKeypointDelegate(ref Image<Gray, Byte> frame, out MKeyPoint[] all_keypoints);

        /// <summary>
        /// Delegate function external hosts may use to replace default BRISK keypoint extraction.
        /// </summary>
        public ExtractKeypointDelegate ExtractKeypoints;

        /// <summary>
        /// Wrapper function for keypoint extraction. If <i>this.ExtractKeypoints != null</i>, it is called. Otherwise, the default BRISK keypoint extractor is invoked.
        /// </summary>
        /// <param name="frame">The grayscale image of the current frame.</param>
        /// <param name="detector">The previously initialized keypoint detector object.</param>
        /// <param name="all_keypoints">The keypoints extracted from the given frame.</param>
        private void ExtractKeypointsFromImage(ref Image<Gray, Byte> frame, IKeyPointDetector detector, out MKeyPoint[] all_keypoints)
        {
            if (ExtractKeypoints == null)
            {
                ExtractBRISKKeypoints(ref frame, detector, out all_keypoints);
            }
            else
            {
                ExtractKeypoints(ref frame, out all_keypoints);
            }
        }
        
        
        /// <summary>
        /// Extracts keypoints from the given frame, using the BRISK keypoint extraction routine.
        /// </summary>
        /// <param name="frame">The grayscale image of the current frame.</param>
        /// <param name="detector">The previously initialized keypoint detector object.</param>
        /// <param name="all_keypoints">The keypoints extracted from the given frame.</param>
        private static void ExtractBRISKKeypoints(ref Image<Gray, Byte> frame, IKeyPointDetector detector, out MKeyPoint[] all_keypoints)
        {
            Console.WriteLine("Internal BRISK Keypoint Detector");
            all_keypoints = detector.DetectKeyPoints(frame, null);
        }
        #endregion

        #region -- Keypoint Descriptors -- Delegates
        /// <summary>
        /// Delegate function defining the interface to an external keypoint descriptor computation.
        /// </summary>
        /// <param name="frame">The grayscale image of the current frame.</param>
        /// <param name="keypoints">Array of keypoints previously extracted from the frame.</param>
        /// <param name="features">The descriptor features extracted from the given frame, based on the given keypoints.</param>
        public delegate void ExtractKeypointDescriptorDelegate(ref Image<Gray, Byte> frame, MKeyPoint[] keypoints, out Matrix<byte> features);

        /// <summary>
        /// Delegate function external hosts may use to replace default BRISK keypoint descriptor computation.
        /// </summary>
        public ExtractKeypointDescriptorDelegate ExtractKeypointDescriptors;

        /// <summary>
        /// Wrapper function for keypoint descriptor computation. If <i>this.ExtractKeypointDescriptors != null</i>, it is called. Otherwise, the default BRISK keypoint descriptor computation is invoked.
        /// </summary>
        /// <param name="frame">The grayscale image of the current frame.</param>
        /// <param name="detector">The previously initialized keypoint extractor object.</param>
        /// <param name="keypoints">Array of keypoints previously extracted from the frame.</param>
        /// <param name="features">The descriptor features extracted from the given frame, based on the given keypoints.</param>
        private void ExtractDescriptorsFromKeypoints(ref Image<Gray, Byte> frame, Feature2DBase<byte> detector, MKeyPoint[] keypoints, out Matrix<byte> features)
        {
            if (ExtractKeypointDescriptors == null)
            {
                ExtractBRISKDescriptors(ref frame, detector, keypoints, out features);
            }
            else
            {
                ExtractKeypointDescriptors(ref frame, keypoints, out features);
            }
        }

        /// <summary>
        /// Computes keypoint descriptors from the given frame, using the BRISK keypoint descriptor computation.
        /// </summary>
        /// <param name="frame">The grayscale image of the current frame.</param>
        /// <param name="detector">The previously initialized keypoint extractor object.</param>
        /// <param name="keypoints">Array of keypoints previously extracted from the frame.</param>
        /// <param name="features">The descriptor features extracted from the given frame, based on the given keypoints.</param>
        private static void ExtractBRISKDescriptors(ref Image<Gray, Byte> frame, Feature2DBase<byte> detector, MKeyPoint[] keypoints, out Matrix<byte> features)
        {
            Console.WriteLine("Internal BRISK Keypoint Descriptor Extractor");
            Emgu.CV.Util.VectorOfKeyPoint keypoint_vector = new Emgu.CV.Util.VectorOfKeyPoint();
            keypoint_vector.Push(keypoints);
            features = detector.ComputeDescriptorsRaw(frame, null, keypoint_vector);
        }
        #endregion

        #region Constructor and Class Initializers
        /// <summary>
        /// Default public constructor. Initializes members and properties to the defaults.
        /// </summary>
        public CMT()
        {
            ClassInit();
        }

        /// <summary>
        /// Initializes all members and properties to their defaults
        /// </summary>
        private void ClassInit()
        {
            mMatchingConfidenceScaleFactor = 512;
            mClusterOutlierThreshold = 20;
            mMatchingConfidenceThreshold = 0.7F;
            mMatchingConfidenceRatio = 0.8F;
            mOpticalFlowErrorThreshold = 20.0F;

            bEstimateRotation = false;
            bEstimateScale = true;

            Reset();
        }

        /// <summary>
        /// Resets all non-configuration members and properties to their defaults, resetting the tracking status to undefined.
        /// </summary>
        public void Reset()
        {
            mFeatureDetector = new Brisk(30, 3, 1.0f);
            mObjectMatcher = new BruteForceMatcher<byte>(DistanceType.Hamming);
            mGlobalMatcher = new BruteForceMatcher<byte>(DistanceType.Hamming);
            
            mCenterTopLeft = new PointF();
            mCenterTopRight = new PointF();
            mCenterBottomRight = new PointF();
            mCenterBottomLeft = new PointF();

            bInitialized = false;
            bValid = false;

            mRotationEstimate = 0.0F;
            mScaleEstimate = 1.0F;

            mBoundingBox = new RectangleF();
            mCenter = new PointF();

            mAllKeypoints = new List<PointF>();
            mAllKeypointFeatures = null;
            mAllKeypointClasses = new List<int>();
            mInitialKeypointCount = 0;

            mActiveKeypoints = new List<PointF>();
            mActiveKeypointFeatures = null;
            mActiveKeypointClasses = new List<int>();

            mSelectedKeypoints = new List<PointF>();
            mSelectedKeypointFeatures = null;
            mSelectedKeypointClasses = new List<int>();
            mSelectedKeypointDistances = null;
            mSelectedKeypointAngles = null;

            mSprings = new List<PointF>();

            mTrackedKeypoints = new List<PointF>();
            mInliers = new List<PointF>();
            mOutliers = new List<PointF>();

            if (mPreviousFrame != null) { mPreviousFrame.Dispose(); }
            mPreviousFrame = null;
        }
        #endregion

        #region CMT Initialization Routines
        /// <summary>
        /// Initializes the CMT tracker on the contents of given ROI in the given frame.
        /// </summary>
        /// <param name="frame">The frame to be used for tracker initialization.</param>
        /// <param name="ROI">The ROI, defined in integer coordinates, to be used for tracker initialization.</param>
        public void Initialize(Image<Bgr, Byte> frame, Rectangle ROI)
        {
            Initialize(frame.Convert<Gray, Byte>(), new RectangleF(ROI.Left, ROI.Top, ROI.Width, ROI.Height));
        }
        /// <summary>
        /// Initializes the CMT tracker on the contents of given ROI in the given frame.
        /// </summary>
        /// <param name="frame">The frame to be used for tracker initialization.</param>
        /// <param name="ROI">The ROI, defined in floating-point coordinates, to be used for tracker initialization.</param>
        public void Initialize(Image<Gray, Byte> frame, Rectangle ROI)
        {
            Initialize(frame, new RectangleF(ROI.Left, ROI.Top, ROI.Width, ROI.Height));
        }
        /// <summary>
        /// <i>Private method, core implementation.</i> Initializes the CMT tracker on the contents of given ROI in the given frame.
        /// </summary>
        /// <param name="frame">The frame to be used for tracker initialization.</param>
        /// <param name="ROI">The ROI, defined in floating-point coordinates, to be used for tracker initialization.</param>
        private void Initialize(Image<Gray, Byte> frame, RectangleF ROI)
        {
            MKeyPoint[] all_keypoints;
            Matrix<byte> all_features;
            Matrix<int> all_classes;
            Matrix<byte> object_features;
            Matrix<int> object_classes;
            Matrix<byte> background_features;
            Matrix<int> background_classes;

            // Get Keypoints in the whole image
            ExtractKeypointsFromImage(ref frame, mFeatureDetector, out all_keypoints);

            List<MKeyPoint> object_keypoints = new List<MKeyPoint>();
            List<MKeyPoint> background_keypoints = new List<MKeyPoint>();
            for(int kpi = 0; kpi < all_keypoints.Length; kpi++)
            {
                MKeyPoint kp = all_keypoints[kpi];
                if (InRect(ROI, kp.Point))
                {
                    // Save the keypoints which fall within the given ROI as selected keypoints
                    object_keypoints.Add(kp);
                }
                else
                {
                    // Save the keypoints that are NOT in the ROI, as background keypoints
                    background_keypoints.Add(kp);
                }
            }

            // Assign each keypoint a class, starting at 1;  Background keypoints are assigned 0
            int NumKeypoints = all_keypoints.Length;
            int NumObjectKeypoints = object_keypoints.Count;
            int NumBackgroundKeypoints = (background_keypoints.Count > 0) ? background_keypoints.Count : 1; //test

            // Collect information about object keypoints
            ExtractDescriptorsFromKeypoints(ref frame, mFeatureDetector, object_keypoints.ToArray(), out object_features);
            object_classes = new Matrix<int>(1, NumObjectKeypoints);
            for (int i = 0; i < NumObjectKeypoints; i++) { object_classes[0, i] = i + 1; }

            // Collect information about background keypoints
            ExtractDescriptorsFromKeypoints(ref frame, mFeatureDetector, background_keypoints.ToArray(), out background_features);
            background_classes = new Matrix<int>(1, NumBackgroundKeypoints);

            // Stack the features and classes
            all_features = background_features.ConcateVertical(object_features);
            all_classes = background_classes.ConcateHorizontal(object_classes);

            // Get distances between object keypoints
            Matrix<float> pdists = new Matrix<float>(NumObjectKeypoints, NumObjectKeypoints);
            Matrix<float> angles = new Matrix<float>(NumObjectKeypoints, NumObjectKeypoints);
            float centerX = 0;
            float centerY = 0;
            for (int i = 0; i < NumObjectKeypoints; i++)
            {
                MKeyPoint p1 = object_keypoints[i];
                centerX += p1.Point.X;
                centerY += p1.Point.Y;

                for (int j = 0; j < NumObjectKeypoints; j++)
                {
                    if (i != j)
                    {
                        MKeyPoint p2 = object_keypoints[j];

                        float d0 = p2.Point.X - p1.Point.X;
                        float d1 = p2.Point.Y - p1.Point.Y;

                        pdists[i, j] = (float)Math.Sqrt(Math.Pow(d0, 2) + Math.Pow(d1, 2));
                        angles[i, j] = (float)Math.Atan2(d1, d0);
                    }
                }
            }

            /*************************************/
            /*************************************/
            /*************************************/

            #region Update property members
            // Store initial image for tracking
            mPreviousFrame = frame.Copy();

            // Set "All" and "Active" Keypoint lists
            mAllKeypoints = new List<PointF>();
            mAllKeypointFeatures = all_features;
            mAllKeypointClasses = new List<int>();

            mActiveKeypoints = new List<PointF>();
            mActiveKeypointFeatures = object_features;
            mActiveKeypointClasses = new List<int>();
            // All and Active Keypoints
            for (int kpi = 0; kpi < all_keypoints.Length; kpi++)
            {
                MKeyPoint kp = all_keypoints[kpi];
                if (ROI.Contains(kp.Point))
                {
                    mActiveKeypoints.Add(kp.Point);
                }
                mAllKeypoints.Add(kp.Point);
            }

            mSelectedKeypoints = mActiveKeypoints;
            mSelectedKeypointFeatures = mActiveKeypointFeatures;
            mSelectedKeypointClasses = mActiveKeypointClasses;

            // Active Keypoint classes
            for (int oc = 0; oc < object_classes.Cols; oc++)
            {
                mActiveKeypointClasses.Add(object_classes[0, oc]);
            }
            // All Keypoint classes
            for (int ac = 0; ac < all_classes.Cols; ac++)
            {
                mAllKeypointClasses.Add(all_classes[0, ac]);
            }

            // Set initial keypoint count
            mInitialKeypointCount = NumObjectKeypoints;

            // Find the center of the object keypoints
            mCenter = new PointF(centerX / NumObjectKeypoints,
                                 centerY / NumObjectKeypoints);

            // Find ROI-bound coordinates, relative to center
            mCenterTopLeft = new PointF(ROI.Left - mCenter.X, ROI.Top - mCenter.Y);
            mCenterTopRight = new PointF(ROI.Right - mCenter.X, ROI.Top - mCenter.Y);
            mCenterBottomRight = new PointF(ROI.Right - mCenter.X, ROI.Bottom - mCenter.Y);
            mCenterBottomLeft = new PointF(ROI.Left - mCenter.X, ROI.Bottom - mCenter.Y);
            
            // Calculate the "springs" of each keypoint
            mSprings = new List<PointF>();
            for (int k = 0; k < NumObjectKeypoints; k++)
            {
                mSprings.Add(new PointF(object_keypoints[k].Point.X - mCenter.X, 
                                        object_keypoints[k].Point.Y - mCenter.Y));
            }

            // Save the Point-to-Point Distances and Angles
            mSelectedKeypointDistances = pdists;
            mSelectedKeypointAngles = angles;

            // Initialize the matcher
            mGlobalMatcher = new BruteForceMatcher<byte>(DistanceType.Hamming);
            mGlobalMatcher.Add(mAllKeypointFeatures);

            mObjectMatcher = new BruteForceMatcher<byte>(DistanceType.Hamming);
            mObjectMatcher.Add(mSelectedKeypointFeatures);

            // Set the 'Initialized' Flag
            bInitialized = true;

            // Set the bounding box
            mBoundingBox = new Rectangle((int)ROI.Left, (int)ROI.Top, (int)ROI.Width, (int)ROI.Height);
            bValid = true;
            #endregion
        }
        #endregion

        #region Track & Estimate
        /// <summary>
        /// Tracks the set of keypoints between frames using optical flow.
        /// </summary>
        /// <param name="prevFrame">The previous grayscale frame.</param>
        /// <param name="curFrame">The current grayscale frame.</param>
        /// <param name="keyPts">The array of keypoints to be tracked.</param>
        /// <param name="classes">The keypoint classes corresponding to each point to be tracked</param>
        /// <param name="errThreshold">The maximum allowed error threshold in reverse-computed optical flow for a point to be considered successfully tracked.</param>
        /// <param name="keypoints_tracked">The array of keypoints which were successfully tracked/</param>
        /// <param name="keypoint_classes">The array of keypoint classes corresponding to each keypoint that was successfully tracked.</param>
        private void Track(Image<Gray, Byte> prevFrame, Image<Gray, Byte> curFrame,
                                  PointF[] keyPts, int[] classes, float errThreshold,
                                  out PointF[] keypoints_tracked, out int[] keypoint_classes)
        {
            int NumKeypoints = keyPts.Length;
            List<PointF> outPts = new List<PointF>();
            List<int> outClasses = new List<int>();

            if (NumKeypoints > 0)
            {
                PointF[] newPts;
                PointF[] bckPts;
                float fb_errX;
                float fb_errY;

                ComputeOpticalFlow(prevFrame, curFrame, keyPts, NumKeypoints, out newPts, out bckPts);
                ComputeOpticalFlowError(keyPts, NumKeypoints, bckPts, out fb_errX, out fb_errY);

                // Filter out large tracking errors
                for (int i = 0; i < NumKeypoints; i++)
                {
                    bool large_fb_error = (fb_errX > errThreshold) || (fb_errY > errThreshold);
                    if (!large_fb_error)
                    {
                        outPts.Add(newPts[i]);
                        outClasses.Add(classes[i]);
                    }
                }
            }
            keypoints_tracked = outPts.ToArray();
            keypoint_classes = outClasses.ToArray();
        }
        
        /// <summary>
        /// Estimate scale, rotation, and object center of tracked object based on tracked keypoints.
        /// </summary>
        /// <param name="tracked_keypoints">(in)The array of keypoints that were tracked. (out)The array of keypoints that were retained as inliers after center-estimation.</param>
        /// <param name="tracked_classes">(in/out)The array of keypoint classes corresponding to the tracked keypoints.</param>
        /// <param name="centerPt">The new estimate of the region center.</param>
        /// <param name="sc_est">The new estimate of the region scale.</param>
        /// <param name="rot_est">The new estimate of the region rotation.</param>
        private void Estimate(ref PointF[] tracked_keypoints, ref int[] tracked_classes,
                                     ref PointF centerPt, ref float sc_est, ref float rot_est)
        {
            centerPt = new PointF(float.NaN, float.NaN);
            sc_est = float.NaN;
            rot_est = float.NaN;
            
            int numKP = tracked_keypoints.Length;
            if (numKP > 1)
            {
                //// Added by Matthew Cotter
                //ClusterKeypoints(ref tracked_keypoints, ref tracked_classes);

                EstimateScaleAndRotation(tracked_keypoints, tracked_classes, ref sc_est, ref rot_est);

                centerPt = EstimateCenter(ref tracked_keypoints, ref tracked_classes, sc_est, rot_est);
            }
            mTrackedKeypoints = new List<PointF>();
            mTrackedKeypoints.AddRange(tracked_keypoints);
        }

        /// <summary>
        /// Estimate scale and rotation of tracked object based on tracked keypoints.
        /// </summary>
        /// <param name="tracked_keypoints">The array of keypoints that were tracked.</param>
        /// <param name="tracked_classes">The array of keypoint classes corresponding to the tracked keypoints.</param>
        /// <param name="sc_est">The new estimate of the region scale.</param>
        /// <param name="rot_est">The new estimate of the region rotation.</param>
        private void EstimateScaleAndRotation(PointF[] tracked_keypoints, int[] tracked_classes, ref float sc_est, ref float rot_est)
        {
            int numKP = tracked_keypoints.Length;
            List<int> sortedTrackOrder = IndexSort<int>(tracked_classes);
            //DumpList(ReorderList(tracked_keypoints, sortedTrackOrder));
            //DumpList(ReorderList(tracked_classes, sortedTrackOrder));

            List<float> distances = new List<float>();
            List<float> scaleChange = new List<float>();
            List<float> angleChange = new List<float>();
            List<PointF> points1 = new List<PointF>();
            List<PointF> points2 = new List<PointF>();
            List<int> class1 = new List<int>();
            List<int> class2 = new List<int>();
            for (int i = 0; i < numKP; i++)
            {
                PointF p1 = tracked_keypoints[sortedTrackOrder[i]];
                int c1 = tracked_classes[sortedTrackOrder[i]];
                for (int j = 0; j < numKP; j++)
                {
                    if (i != j)
                    {
                        PointF p2 = tracked_keypoints[sortedTrackOrder[j]];
                        int c2 = tracked_classes[sortedTrackOrder[j]];
                        if (c2 != c1)
                        {
                            // Compute distance
                            float dist = L2Norm(p1, p2);
                            float deltaS = dist / mSelectedKeypointDistances[c1 - 1, c2 - 1];
                            distances.Add(dist);
                            scaleChange.Add(deltaS);

                            // Compute angle
                            float angle = PointAngle(p1, p2);
                            float deltaA = BoundAngle(angle - mSelectedKeypointAngles[c1 - 1, c2 - 1]);
                            angleChange.Add(deltaA);
                        }
                    }
                }
            }
            scaleChange.Sort();
            angleChange.Sort();
            int itemCount = scaleChange.Count;
            if (itemCount == 0)
            {
                return;
            }
            if (itemCount % 2 == 0)
            {
                // Even number of elements, average the two middle-most entries
                int medianIndexLo = scaleChange.Count / 2 - 1;
                int medianIndexHi = medianIndexLo + 1;
                sc_est = (float)((scaleChange[medianIndexLo] + scaleChange[medianIndexHi]) / 2.0);
                rot_est = (float)((angleChange[medianIndexLo] + angleChange[medianIndexHi]) / 2.0);
            }
            else
            {
                // Odd number of elements, take the middle entry
                int medianIndex = (int)Math.Floor((double)scaleChange.Count / 2);
                sc_est = scaleChange[medianIndex];
                rot_est = angleChange[medianIndex];
            }
            if (!bEstimateScale)
            {
                sc_est = 1;
            }
            if (!bEstimateRotation)
            {
                rot_est = 0;
            }
            //DumpList(class_ind1, 25);
            //DumpList(class_ind2, 25);
        }
        
        /// <summary>
        /// Estimate object center of tracked object based on tracked keypoints
        /// </summary>
        /// <param name="tracked_keypoints">(in)The array of keypoints that were tracked. (out)The array of keypoints that were retained as inliers after center-estimation.</param>
        /// <param name="tracked_classes">(in/out)The array of keypoint classes corresponding to the tracked keypoints.</param>
        /// <param name="sc_est">The new estimate of the region scale.</param>
        /// <param name="rot_est">The new estimate of the region rotation.</param>
        /// <returns>The new estimate of the region center.</returns>
        private PointF EstimateCenter(ref PointF[] tracked_keypoints, ref int[] tracked_classes, 
                                             float sc_est, float rot_est)
        {
            int numKP = tracked_keypoints.Length;

            List<PointF> transformedSprings = Transform(mSprings, sc_est, rot_est);

            // Vote for new object center
            List<PointF> votes = new List<PointF>();
            for (int tkpi = 0; tkpi < numKP; tkpi++)
            {
                PointF pt = tracked_keypoints[tkpi];
                PointF spring = transformedSprings[tracked_classes[tkpi] - 1];
                votes.Add(new PointF(pt.X - spring.X,
                                     pt.Y - spring.Y));
            }

            //DumpPointsAndClasses(votes, tracked_classes);

            bool[] inliers = Clustering.Clusterizer.GetClusterInliers(votes, mClusterOutlierThreshold);

            float centerX = 0.0F;
            float centerY = 0.0F;
            int inlier_count = 0;
            mInliers = new List<PointF>();
            mOutliers = new List<PointF>();
            List<int> inlier_classes = new List<int>();
            for(int c = 0; c < inliers.Length;c++)
            {
                if (inliers[c])
                {
                    PointF v = votes[c];
                    centerX += v.X;
                    centerY += v.Y;
                    inlier_count++;
                    mInliers.Add(tracked_keypoints[c]);
                    inlier_classes.Add(tracked_classes[c]);
                }
                else
                {
                    // Stop tracking outliers
                    mOutliers.Add(tracked_keypoints[c]);
                }
            }
            tracked_keypoints = mInliers.ToArray();
            tracked_classes = inlier_classes.ToArray();
            PointF newCenter = new PointF(centerX / inlier_count, centerY / inlier_count);
            return newCenter;
        }

        /// <summary>
        /// Cluster keypoint center estimates to find the most likely candidate.
        /// </summary>
        /// <param name="center_votes">The array of center point estimates, based on object model and new keypoint locations.</param>
        /// <param name="center_classes">The array of classes corresponding to the keypoints.</param>
        private void ClusterCenterVotes(ref PointF[] center_votes, ref int[] center_classes)
        {
            List<PointF> points = new List<PointF>();
            List<int> classes = new List<int>();
            points.AddRange(center_votes);
            classes.AddRange(center_classes);

            bool[] inliers = Clustering.Clusterizer.GetClusterInliers(points, classes, mBoundingBox.Width / 2);

            List<PointF> inlier_points = new List<PointF>();
            List<int> inlier_classes = new List<int>();
            for (int c = 0; c < inliers.Length; c++)
            {
                if (inliers[c])
                {
                    inlier_points.Add(center_votes[c]);
                    inlier_classes.Add(center_classes[c]);
                }
            }
            center_votes = inlier_points.ToArray();
            center_classes = inlier_classes.ToArray();
        }
        
        #endregion
        
        #region ProcessFrame
        /// <summary>
        /// Processes the current frame to track an object which was last detected in the given ROI.
        /// </summary>
        /// <param name="frame">The current color image frame, which will be converted to grayscale.</param>
        /// <param name="ROI">The rectangle which defines the region in which the target object was last detected.</param>
        /// <returns>The estimate of the new ROI off the object to be tracked.</returns>
        public Rectangle ProcessFrame(Image<Bgr, Byte> frame, Rectangle ROI)
        {
            return ProcessFrame(frame.Convert<Gray, Byte>(), new RectangleF(ROI.Left, ROI.Top, ROI.Width, ROI.Height));
        }
        /// <summary>
        /// Processes the current frame to track an object which was last detected in the given ROI.
        /// </summary>
        /// <param name="frame">The current grayscale image frame.</param>
        /// <param name="ROI">The rectangle which defines the region in which the target object was last detected.</param>
        /// <returns>The estimate of the new ROI off the object to be tracked.</returns>
        public Rectangle ProcessFrame(Image<Gray, Byte> frame, Rectangle ROI)
        {
            return ProcessFrame(frame, new RectangleF(ROI.Left, ROI.Top, ROI.Width, ROI.Height));
        }
        /// <summary>
        /// <i>Private method, core implementation.</i> Processes the current frame to track an object which was last detected in the given ROI.
        /// </summary>
        /// <param name="frame">The current grayscale image frame.</param>
        /// <param name="ROI">The rectangle which defines the region in which the target object was last detected.</param>
        /// <returns>The estimate of the new ROI off the object to be tracked.</returns>
        private Rectangle ProcessFrame(Image<Gray, Byte> frame, RectangleF ROI)
        {
            if (!Initialized)
            {
                Initialize(frame, ROI);
                return RectFToRect(mBoundingBox);
            }

            PointF[] tracked_keypoints;
            int[] tracked_classes;

            Track(mPreviousFrame, frame, mActiveKeypoints.ToArray(), mActiveKeypointClasses.ToArray(),
                  mOpticalFlowErrorThreshold, out tracked_keypoints, out tracked_classes);

            PointF centerEstimate = mCenter;
            float scaleEstimate = mScaleEstimate;
            float rotationEstimate = mRotationEstimate;
            Estimate(ref tracked_keypoints, ref tracked_classes,
                     ref centerEstimate, ref scaleEstimate, ref rotationEstimate);
            bool isCenterNaN = float.IsNaN(centerEstimate.X) || float.IsNaN(centerEstimate.Y);

            List<PointF> updatedKeypoints = new List<PointF>();
            List<int> updatedKeypointClasses = new List<int>();
            List<Matrix<byte>> updatedKeypointFeatures = new List<Matrix<byte>>();
            List<float> updatedConfidences = new List<float>();

            // Process frame for new keypoints
            MKeyPoint[] keypoint_vector;
            ExtractKeypointsFromImage(ref frame, mFeatureDetector, out keypoint_vector);
            Matrix<byte> keypoint_features;
            ExtractDescriptorsFromKeypoints(ref frame, mFeatureDetector, keypoint_vector, out keypoint_features);

            int k = 2;
            Matrix<int> matches_all = new Matrix<int>(keypoint_features.Rows, k);
            Matrix<float> matches_all_distances = new Matrix<float>(keypoint_features.Rows, k);
            Matrix<int> selected_matches = null;
            Matrix<float> selected_matches_distances = null;

            // Match newly detected keypoints against all keypoints
            mGlobalMatcher.KnnMatch(keypoint_features, matches_all, matches_all_distances, 2, null);
            if (!isCenterNaN)
            {
                // Match newly detected keypoints againt object model keypoints
                selected_matches = new Matrix<int>(keypoint_features.Rows, mActiveKeypoints.Count);
                selected_matches_distances = new Matrix<float>(keypoint_features.Rows, mSelectedKeypoints.Count);
                mObjectMatcher.KnnMatch(keypoint_features, selected_matches, selected_matches_distances, mSelectedKeypoints.Count, null);
            }

            if (keypoint_vector.Length > 0)
            {
                List<PointF> transformedSprings = Transform(mSprings, scaleEstimate, -rotationEstimate);
                for (int kpvi = 0; kpvi < keypoint_vector.Length; kpvi++)
                {
                    PointF location = keypoint_vector[kpvi].Point;
                    Matrix<byte> features = keypoint_features.GetRow(kpvi);
                    Matrix<int> matches = matches_all.GetRow(kpvi);
                    Matrix<float> distances = matches_all_distances.GetRow(kpvi);
                    Matrix<float> confidences = 1 - (distances / mMatchingConfidenceScaleFactor);

                    List<int> matchList = new List<int>();
                    List<float> distList = new List<float>();
                    for (int d = 0; d < matches.Width; d++)
                    {
                        matchList.Add(matches[0, d]);
                        distList.Add(distances[0, d]);
                    }
                    List<int> sortedOrder = IndexSort<float>(distList, true);
                    matchList = ReorderList<int>(matchList, sortedOrder);
                    distList = ReorderList<float>(distList, sortedOrder);
                    for (int d = 0; d < matches.Width; d++)
                    {
                        distances[0, d] = distList[d];
                    }

                    int bestInd = matchList[0];
                    int secondBestInd = matchList[1];
                    float ratio = (1 - confidences[0, 0]) / (1 - confidences[0, 1]);
                    int keypoint_class = mAllKeypointClasses[bestInd];

                    float confidence = confidences[0, 0];
                    if ((ratio < mMatchingConfidenceRatio) &&
                        (confidence > mMatchingConfidenceThreshold) && 
                        (keypoint_class != 0))
                    {
                        while (updatedKeypointClasses.Contains(keypoint_class))
                        {
                            int index = updatedKeypointClasses.IndexOf(keypoint_class);
                            updatedKeypointClasses.RemoveAt(index);
                            updatedKeypoints.RemoveAt(index);
                            updatedKeypointFeatures.RemoveAt(index);
                            updatedConfidences.RemoveAt(index);
                        }
                        updatedKeypoints.Add(location);
                        updatedKeypointClasses.Add(keypoint_class);
                        updatedKeypointFeatures.Add(features);
                        updatedConfidences.Add(confidence);
                    }
                    if (!isCenterNaN)
                    { 
                        matches = selected_matches.GetRow(kpvi);
                        distances = selected_matches_distances.GetRow(kpvi);
                        matchList = new List<int>();
                        distList = new List<float>();
                        for(int d = 0; d < matches.Width; d++)
                        {
                            matchList.Add(matches[0, d]);
                            distList.Add(distances[0, d]);
                        }
                        sortedOrder = IndexSort<float>(distList, true);
                        matchList = ReorderList<int>(matchList, sortedOrder);
                        distList = ReorderList<float>(distList, sortedOrder);
                        for (int d = 0; d < matches.Width; d++)
                        {
                            distances[0, d] = distList[d];
                        }


                        confidences = 1 - (distances / mMatchingConfidenceScaleFactor);
                        
                        PointF relative_location = new PointF(location.X - centerEstimate.X, location.Y - centerEstimate.Y);
                        List<float> weightedConf = new List<float>();
                        for (int s = 0; s < matchList.Count; s++)
                        {
                            int cIdx = matchList[s];
                            //# Compute the distances to all springs
                            float displacement = L2Norm(transformedSprings[cIdx], relative_location);

                            //# For each spring, calculate weight
                            float weight = (displacement < mClusterOutlierThreshold ? 1 : 0);
                            weightedConf.Add(confidences[0, s] * weight);
                        }
                        
					    //# Sort in descending order
                        sortedOrder = IndexSort<float>(weightedConf, false);

                        //# Get best and second best index
                        bestInd = matchList[sortedOrder[0]];
                        secondBestInd = matchList[sortedOrder[1]];

					    //# Compute distance ratio according to Lowe
                        ratio = (1 - weightedConf[sortedOrder[0]]) / (1 - weightedConf[sortedOrder[1]]);
                        
					    //# Extract class of best match
                        keypoint_class = mSelectedKeypointClasses[bestInd];

                        confidence = weightedConf[0];
					    //# If distance ratio is ok and absolute distance is ok and keypoint class is not background
					    if ((ratio < mMatchingConfidenceRatio) &&
                            (confidence > mMatchingConfidenceThreshold) && 
                            (keypoint_class != 0))
                        {
                            if (updatedKeypointClasses.Contains(keypoint_class))
                            {
                                int index = updatedKeypointClasses.IndexOf(keypoint_class);
                                if (updatedConfidences[index] < confidence)
                                {
                                    while (updatedKeypointClasses.Contains(keypoint_class))
                                    {
                                        index = updatedKeypointClasses.IndexOf(keypoint_class);
                                        updatedKeypointClasses.RemoveAt(index);
                                        updatedKeypoints.RemoveAt(index);
                                        updatedKeypointFeatures.RemoveAt(index);
                                        updatedConfidences.RemoveAt(index);
                                    }
                                    updatedKeypoints.Add(location);
                                    updatedKeypointClasses.Add(keypoint_class);
                                    updatedKeypointFeatures.Add(features);
                                    updatedConfidences.Add(confidence);
                                }
                            }
                        }
                    }
                }
            }
            // Add all tracked, but unmatched keypoints
            for (int tkp = 0; tkp < tracked_keypoints.Length; tkp++)
            {
                int keypoint_class = tracked_classes[tkp];
                if (!updatedKeypointClasses.Contains(keypoint_class))
                {
                    PointF location = tracked_keypoints[tkp];
                    Matrix<byte> features = mActiveKeypointFeatures.GetRow(tkp);
                    updatedKeypoints.Add(location);
                    updatedKeypointClasses.Add(keypoint_class);
                    updatedKeypointFeatures.Add(features);
                }
            }


            mActiveKeypoints = updatedKeypoints;
            mActiveKeypointClasses = updatedKeypointClasses;

            mPreviousFrame.Dispose();
            mPreviousFrame = frame;

            mCenter = centerEstimate;
            mScaleEstimate = scaleEstimate;
            mRotationEstimate = rotationEstimate;

            bValid = false;
            // Update state estimate
            if ((!isCenterNaN) && (mActiveKeypoints.Count > ((float)mInitialKeypointCount / 10.0)))
            {
                bValid = true;

                PointF centerTopLeft = Transform(mCenterTopLeft, mScaleEstimate, -mRotationEstimate);
                PointF centerTopRight = Transform(mCenterTopRight, mScaleEstimate, -mRotationEstimate);
                PointF centerBottomRight = Transform(mCenterBottomRight, mScaleEstimate, -mRotationEstimate);
                PointF centerBottomLeft = Transform(mCenterBottomLeft, mScaleEstimate, -mRotationEstimate);
                
                PointF TopLeft = new PointF(mCenter.X + centerTopLeft.X, mCenter.Y + centerTopLeft.Y);
                PointF TopRight = new PointF(mCenter.X + centerTopRight.X, mCenter.Y + centerTopRight.Y);
                PointF BottomLeft = new PointF(mCenter.X + centerBottomLeft.X, mCenter.Y + centerBottomLeft.Y);
                PointF BottomRight = new PointF(mCenter.X + centerBottomRight.X, mCenter.Y + centerBottomRight.Y);

                int minX = (int)(new float[] { TopLeft.X, TopRight.X, BottomLeft.X, BottomRight.X }).Min();
                int maxX = (int)(new float[] { TopLeft.X, TopRight.X, BottomLeft.X, BottomRight.X }).Max();
                int minY = (int)(new float[] { TopLeft.Y, TopRight.Y, BottomLeft.Y, BottomRight.Y }).Min();
                int maxY = (int)(new float[] { TopLeft.Y, TopRight.Y, BottomLeft.Y, BottomRight.Y }).Max();

                mBoundingBox = new Rectangle(minX, minY, (maxX - minX), (maxY - minY));

            }

            return RectFToRect(mBoundingBox);
        }

        #endregion

        #region Static Methods
        /// <summary>
        /// Function to determine whether a point falls within the given bounding box.
        /// </summary>
        /// <param name="rect">The rectangle which defines the bounding box.</param>
        /// <param name="pt">The point to be tested.</param>
        /// <returns>True if the point lies inside the bounding box. False, otherwise.</returns>
        public bool InRect(RectangleF rect, PointF pt)
        {
	        bool C1 = pt.X > rect.Left;
	        bool C2 = pt.Y > rect.Top;
	        bool C3 = pt.X < rect.Right;
            bool C4 = pt.Y < rect.Bottom;
        
	        return C1 & C2 & C3 & C4;
        }

        /// <summary>
        /// Computes the angle, in radians, between two points, using the origin as a common vertex.
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>The angle, in radians, between the two points.</returns>
        private static float PointAngle(PointF p1, PointF p2)
        {
            PointF pointDiff = new PointF(p1.X - p2.X, p1.Y - p2.Y);
            PointF v = new PointF(-pointDiff.X, -pointDiff.Y);
            float angle = (float)Math.Atan2(v.Y, v.X);
            return angle;
        }
        
        /// <summary>
        /// Bounds an angle, in radians, to be between +/- PI
        /// </summary>
        /// <param name="deltaA">The input angle, in radians.</param>
        /// <returns>The same angle, in radians, such that -Math.PI &lt;= angle &lt;= Math.Pi</returns>
        private static float BoundAngle(float deltaA)
        {
            deltaA = (float)(deltaA > Math.PI ? deltaA - (2 * Math.PI) : deltaA);
            deltaA = (float)(deltaA < -Math.PI ? deltaA + (2 * Math.PI) : deltaA);
            return deltaA;
        }
        
        /// <summary>
        /// Computes the L2-norm distance between two points.
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>The L2-norm distance between the two points.</returns>
        private static float L2Norm(PointF p1, PointF p2)
        {
            double delta1 = p2.X - p1.X;
            double delta2 = p2.Y - p1.Y;
            double d12 = Math.Pow(delta1, 2);
            double d22 = Math.Pow(delta2, 2);
            float dist = (float)Math.Sqrt(d12 + d22);
            return dist;
        }

        /// <summary>
        /// Transforms the given point using rotational followed by scale transformations.
        /// </summary>
        /// <param name="pt">The point to be transformed.</param>
        /// <param name="scale">The magnitude of the scale transformation. (1.0 is no change)</param>
        /// <param name="radians">The angle, in radians, of the rotation transformation. (0 radians is no change)</param>
        /// <returns>The transformed (rotated and scaled) point.</returns>
        private static PointF Transform(PointF pt, float scale, float radians)
        {
            PointF rotatedPt;
            if (radians == 0)
            {
                rotatedPt = pt;
            }
            else 
            {
                float s = (float)Math.Sin(radians);
                float c = (float)Math.Cos(radians);
                rotatedPt = new PointF((c * pt.X) - (s * pt.Y),
                                       (s * pt.X) + (c * pt.Y));
            }
            PointF scaledPt = new PointF(scale * rotatedPt.X, scale * rotatedPt.Y);
            return scaledPt;
        }

        /// <summary>
        /// Transforms the given list of points using rotational followed by scale transformations.
        /// </summary>
        /// <param name="pts">The list of points to be transformed.</param>
        /// <param name="scale">The magnitude of the scale transformation. (1.0 is no change)</param>
        /// <param name="radians">The angle, in radians, of the rotation transformation. (0 radians is no change)</param>
        /// <returns>The transformed (rotated and scaled) list of points.</returns>
        private static List<PointF> Transform(List<PointF> pts, float scale, float radians)
        {
            List<PointF> transformedPoints = new List<PointF>();
            if (radians == 0)
            {
                transformedPoints.AddRange(pts);
                return transformedPoints;
            }
            float s = (float)Math.Sin(radians);
            float c = (float)Math.Cos(radians);
            for (int p = 0; p < pts.Count; p++)
            {
                PointF newP = new PointF(scale * ((c * pts[p].X) - (s * pts[p].Y)),
                                         scale * ((s * pts[p].X) + (c * pts[p].Y)));
                transformedPoints.Add(newP);
            }
            return transformedPoints;
        }

        /// <summary>
        /// Gets the indices of the sorted elements of the array of a given type.
        /// </summary>
        /// <typeparam name="T">The type of objects in the array to be index-sorted.</typeparam>
        /// <param name="items">The array of items to be index-sorted.</param>
        /// <param name="Ascending">Boolean flag indicating whether items should be sorted in ascending order (True), or descending order (False)</param>
        /// <returns>The indices of the input array, which represent the original array in sorted order by value.</returns>
        private static List<int> IndexSort<T>(T[] items, bool Ascending = true)
        {
            List<T> listItems = new List<T>();
            listItems.AddRange(items.ToArray());
            return IndexSort<T>(listItems, Ascending);
        }

        /// <summary>
        /// Gets the indices of the sorted elements of the list of a given type.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list to be index-sorted.</typeparam>
        /// <param name="items">The list of items to be index-sorted.</param>
        /// <param name="Ascending">Boolean flag indicating whether items should be sorted in ascending order (True), or descending order (False)</param>
        /// <returns>The indices of the input list, which represent the original array in sorted order by value.</returns>
        private static List<int> IndexSort<T>(List<T> items, bool Ascending = true)
        {
            List<int> indices = new List<int>();
            if (Ascending)
            {
                var sortQuery = items.Select((value, index) => new { value, index });
                var tieOrder = from entry in sortQuery
                               orderby entry.value, entry.index
                               select entry.index;
                indices.AddRange(tieOrder.ToArray());
                
            }
            else
            {
                var sortQuery = items.Select((value, index) => new { value, index });
                var tieOrder = from entry in sortQuery
                               orderby entry.value descending, entry.index descending
                               select entry.index;
                indices.AddRange(tieOrder.ToArray());
            }
            return indices;
        }

        /// <summary>
        /// Reorders a given array, by placing the items in order based on the values of order.
        /// </summary>
        /// <typeparam name="T">The type of objects in the array to be index-sorted.</typeparam>
        /// <param name="source">The array to be reordered.</param>
        /// <param name="order">The array of indices which indicate the order of the reordered array.</param>
        /// <returns>The reordered array.</returns>
        private static T[] ReorderList<T>(T[] items, List<int> order)
        {
            List<T> listItems = new List<T>();
            listItems.AddRange(items.ToArray());
            return ReorderList<T>(listItems, order).ToArray();
        }

        /// <summary>
        /// Reorders a given list, by placing the items in order based on the values of order.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list to be index-sorted.</typeparam>
        /// <param name="source">The list to be reordered.</param>
        /// <param name="order">The list of indices which indicate the order of the reordered array.</param>
        /// <returns>The reordered list.</returns>
        private static List<T> ReorderList<T>(List<T> source, List<int> order)
        {
            List<T> newList = new List<T>();
            for(int i = 0;i < order.Count;i ++)
            {
                newList.Add(source[order[i]]);
            }
            return newList;
        }

        /// <summary>
        /// Converts a point in integer coordinates to a point in floating-point coordinates.
        /// </summary>
        /// <param name="pt">The point in integer coordinates.</param>
        /// <returns>The point in floating-point coordinates.</returns>
        private static Point PointFToPoint(PointF pt)
        {
            return new Point((int)pt.X, (int)pt.Y);
        }
        /// <summary>
        /// Converts the rectangle, in floating-point coordinates, to a rectangle in integer coordinates.
        /// </summary>
        /// <param name="r">The input rectangle, in floating-point coordinates.</param>
        /// <returns>The new rectangle, in integer coordinates.</returns>
        public static Rectangle RectFToRect(RectangleF r)
        {
            Rectangle rect = new Rectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
            return rect;
        }

        /// <summary>
        /// Converts a point, in floating-point coordinates, to a square, in integer coordinates, with a side length equal to 2 times the given span.
        /// </summary>
        /// <param name="pt">The point in floating-point coordinates.</param>
        /// <returns>The rectangle, centered at <i>pt</i>, with a size of <i>2 * span</i> by <i>2 * span</i>.</returns>
        public static Rectangle PointFToRect(PointF pt, int span)
        {
            Rectangle rect = new Rectangle((int)pt.X - span, (int)pt.Y - span, span * 2, span * 2);
            return rect;
        }
        #endregion
    }
}
