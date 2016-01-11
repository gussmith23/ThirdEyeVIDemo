using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;
using Emgu.CV.Util;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Clustering;

namespace CMT_Tracker
{
    /// <summary>
    /// C# Implementation of Consensus-based Matching and Tracking of Keypoints.
    /// <see cref="http://www.gnebehay.com/cmt/"/>
    /// <see cref="Nebehay, Georg and Pflugfelder, Roman. Consensus-based Matching and Tracking of Keypoints for Object Tracking. Winter Conference on Applications of Computer Vision 2014."/>
    /// </summary>
    class CMT_SURF
    {

        #region Private Members
        //private bool mSURFEnabled = true;
       
        private SURFDetector mSURFDetector;

        private BruteForceMatcher<float> mSurfObjectMatcher;
        private BruteForceMatcher<float> mSurfGlobalMatcher;
        private BruteForceMatcher<float> mAdaptiveSurfBackgroundMatcher;

        private PointF mCenterTopLeft;
        private PointF mCenterTopRight;
        private PointF mCenterBottomRight;
        private PointF mCenterBottomLeft;

        private int frame_count = 1;

        private Stopwatch sw1 = new Stopwatch();    // SURF Keypoints
        private Stopwatch sw2 = new Stopwatch();    // SURF Descriptors
        private Stopwatch sw3 = new Stopwatch();    // Background Matcher
        private Stopwatch sw4 = new Stopwatch();    // Background Match Logic
        private Stopwatch sw5 = new Stopwatch();    // Object Matcher
        private Stopwatch sw6 = new Stopwatch();    // Object Match Logic
        private Stopwatch sw7 = new Stopwatch();
        private Stopwatch sw8 = new Stopwatch();
        private Stopwatch sw9 = new Stopwatch();
        private Stopwatch sw10 = new Stopwatch();    

        private int total_num_keypoints;
        private int num_object_model_keypoints;
        private int num_bgnd_model_keypoints;
        private int num_poss_object_keypoints;

        #endregion

        #region Private Members of Public Properties
        private float mOpticalFlowErrorThreshold = 20.0F;
        private int mClusterOutlierThreshold = 20;
        private int mMatchingConfidenceScaleFactor = 512;

        private float mSURFMatchingConfidenceThreshold = 0.7F;  // Default is .7
        private float mSURFMatchingConfidenceRatio = 0.8F;        // Default is .8

        //private float mWeightedMaxDistance;
        private float mWeightedDistanceConfThreshold;
        private float mVarianceDistance;
        private float mMinVarianceDistance = 35.0F;
        private float mMeanDistance; 

        private bool bEstimateScale;
        private bool bEstimateRotation;
        private bool bInitialized;
        private bool bValid;

        private float mRotationEstimate;
        private float mScaleEstimate;

        private RectangleF mBoundingBox;
        private PointF mCenter;
        private PointF mPrevCenter;

        // All keypoints in the initial frame
        private List<PointF> mAllInitialKeypoints;
        private Matrix<float> mAllInitialFeatures;
        private List<int> mAllInitialKeypointClasses;
        private int mInitialKeypointCount;

        // Currently active keypoints to be tracked in the frame
        private List<PointF> mActiveKeypoints;
        private Matrix<float> mActiveFeatures;
        private List<int> mActiveKeypointClasses;

        // Model for background keypoints and features
        private List<MKeyPoint> mInitialBackgroundKeypoints;
        private Matrix<float> mInitialBackgroundFeatures;
        private List<int> mInitialBackgroundKeypointClasses;

        // Model object keypoints from the initial frame
        private List<PointF> mSelectedKeypoints;
        private List<MKeyPoint> mSelectedMKeypoints;
        private List<int> mSelectedKeypointClasses;
        private Matrix<float> mSelectedFeatures;
        private Matrix<float> mSelectedKeypointDistances;
        private Matrix<float> mSelectedKeypointAngles;
        private List<PointF> mSprings;

        // Adaptive model of background keypoints in the frame
        private List<MKeyPoint> mAdaptiveBackgroundKeypoints;
        private Matrix<float> mAdaptiveBackgroundFeatures;

        private List<MKeyPoint> mAdaptiveBackgroundKeypointsPrevious;
        private Matrix<float> mAdaptiveBackgroundFeaturesPrevious;

        private MKeyPoint[] mCurrentFrameKeypoints;
        private Matrix<float> mCurrentFrameFeatures;

        private List<PointF> mTrackedKeypoints;
        private List<PointF> mInliers;
        private List<PointF> mOutliers;

        private List<MKeyPoint> mPossibleObjectKeypoints;
        private Matrix<float> mPossibleObjectFeatures;

        private List<MKeyPoint> mMatchedBackgroundKeypoints;
        private List<Matrix<float>> mMatchedBackgroundFeatures;
        private Matrix<int> mMatchedBackgroundMatches;

        private List<MKeyPoint> mHomographyBackgroundKeypoints;
        private Matrix<float> mHomographyBackgroundFeatures;

        private List<PointF> mUpdatedKeypoints;
        private List<MKeyPoint> mUpdatedMKeypoints;
        private List<int> mUpdatedKeypointClasses;
        private List<Matrix<float>> mUpdatedKeypointFeatures;
        private Matrix<float> mUpdatedFeaturesMatrix;
        private List<float> mUpdatedConfidences;
        private List<PointF> mUpdatedKeypointMatchesPoints;
        private List<Matrix<int>> mUpdatedKeypointMatches;
        private List<Matrix<float>> mUpdatedDistances;

        private bool mAdaptiveBackgroundModel;
        private bool mIsCenterNaN;
        private bool mStdDevCalc = false;
        
        private Image<Gray, Byte> mPreviousFrame;
        private Image<Gray, Byte> mOriginalImage;
        private Image<Gray, Byte> mCurrentFrame;

        //private List<RectangleF> mGroundTruthBBs;

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

        public List<MKeyPoint> NonBackgroundPoints { get { return (mPossibleObjectKeypoints == null ? new List<MKeyPoint>() : mPossibleObjectKeypoints); } }

        public List<MKeyPoint> InitialBackgroundKeypoints { get { return (mInitialBackgroundKeypoints == null ? new List<MKeyPoint>() : mInitialBackgroundKeypoints); } }

        //public List<RectangleF> GroundTruthBBs { get { return (mGroundTruthBBs == null ? new List<RectangleF>() : mGroundTruthBBs); } set { mGroundTruthBBs = value; } }
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
        private static void ComputeOpticalFlowError(PointF[] pts, int numPts, PointF[] bckPts, out float[] errX, out float[] errY)
        {
            errX = new float[numPts];
            errY = new float[numPts];
            for (int i = 0; i < numPts; i++)
            {
                errX[i] = (float)Math.Sqrt(Math.Pow(bckPts[i].X - pts[i].X, 2));
                errY[i] = (float)Math.Sqrt(Math.Pow(bckPts[i].Y - pts[i].Y, 2));
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
            //Console.WriteLine("Internal BRISK Keypoint Detector");
            all_keypoints = detector.DetectKeyPoints(frame, null);
        }

        private void ExtractSURFKeypoints(ref Image<Gray, Byte> frame, out MKeyPoint[] all_keypoints)
        {
            //Console.WriteLine("Internal SURF Keypoint Detector");
            all_keypoints = mSURFDetector.DetectKeyPoints(frame, null);
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
            //Console.WriteLine("Internal BRISK Keypoint Descriptor Extractor");
            Emgu.CV.Util.VectorOfKeyPoint keypoint_vector = new Emgu.CV.Util.VectorOfKeyPoint();
            keypoint_vector.Push(keypoints);
            features = detector.ComputeDescriptorsRaw(frame, null, keypoint_vector);
        }

        private void ExtractSURFDescriptors(ref Image<Gray, Byte> frame, MKeyPoint[] keypoints, out Matrix<float> features)
        {
            //Console.WriteLine("Internal SURF Keypoint Descriptor Extractor");
            Emgu.CV.Util.VectorOfKeyPoint keypoint_vector = new Emgu.CV.Util.VectorOfKeyPoint();
            keypoint_vector.Push(keypoints);
            features = mSURFDetector.ComputeDescriptorsRaw(frame, null, keypoint_vector);
        }
        #endregion

        #region Constructor and Class Initializers
        /// <summary>
        /// Default public constructor. Initializes members and properties to the defaults.
        /// </summary>
        public CMT_SURF()
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
            
            mSURFMatchingConfidenceThreshold = 0.999F;   // for adapted l1 = .9 // Default is .7
            mSURFMatchingConfidenceRatio = 0.93F;       // for adapted l1 = .8  // Default is .8
            mOpticalFlowErrorThreshold = 20.0F;
            
            mWeightedDistanceConfThreshold = 0.9F;     // for adapted l2 = .9

            bEstimateRotation = false;
            bEstimateScale = true;
            mAdaptiveBackgroundModel = false;

            Reset();
        }

        /// <summary>
        /// Resets all non-configuration members and properties to their defaults, resetting the tracking status to undefined.
        /// </summary>
        public void Reset()
        {
            
            mSURFDetector = new SURFDetector(400, false);
            mSurfGlobalMatcher = new BruteForceMatcher<float>(DistanceType.L2);
            mSurfObjectMatcher = new BruteForceMatcher<float>(DistanceType.L2);
            mAdaptiveSurfBackgroundMatcher = new BruteForceMatcher<float>(DistanceType.L2);

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

            mAllInitialKeypoints = new List<PointF>();
            mAllInitialFeatures = null;
            mAllInitialKeypointClasses = new List<int>();
            mInitialKeypointCount = 0;

            mAdaptiveBackgroundKeypoints = new List<MKeyPoint>();
            mAdaptiveBackgroundFeatures = null;

            mAdaptiveBackgroundKeypointsPrevious = new List<MKeyPoint>();
            mAdaptiveBackgroundFeaturesPrevious = null;

            mActiveKeypoints = new List<PointF>();
            mActiveFeatures = null;
            mActiveKeypointClasses = new List<int>();

            mSelectedKeypoints = new List<PointF>();
            mSelectedMKeypoints = new List<MKeyPoint>();
            mSelectedKeypointClasses = new List<int>();
            mSelectedKeypointDistances = null;
            mSelectedKeypointAngles = null;
            mSprings = new List<PointF>();

            mTrackedKeypoints = new List<PointF>();
            mInliers = new List<PointF>();
            mOutliers = new List<PointF>();
            
            mPossibleObjectKeypoints = new List<MKeyPoint>();
            mPossibleObjectFeatures = null;

            mMatchedBackgroundKeypoints = new List<MKeyPoint>();
            mMatchedBackgroundFeatures = new List<Matrix<float>>();
            mMatchedBackgroundMatches = null;

            mHomographyBackgroundKeypoints = new List<MKeyPoint>();
            mHomographyBackgroundFeatures = null;

            mUpdatedKeypoints = new List<PointF>();
            mUpdatedMKeypoints = new List<MKeyPoint>();
            mUpdatedKeypointClasses = new List<int>();
            mUpdatedKeypointFeatures = new List<Matrix<float>>();
            mUpdatedFeaturesMatrix = null;
            mUpdatedConfidences = new List<float>();
            mUpdatedKeypointMatchesPoints = new List<PointF>();
            mUpdatedKeypointMatches = new List<Matrix<int>>();
            mUpdatedDistances = new List<Matrix<float>>();

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
            Matrix<float> surf_all_features;
            Matrix<int> all_classes;
            Matrix<float> surf_object_features;
            Matrix<int> object_classes;
            Matrix<float> surf_background_features;
            Matrix<int> background_classes;

            mOriginalImage = frame.Clone();

            //mWeightedMaxDistance = (float)(Math.Sqrt(Math.Pow(frame.Height, 2.0) + Math.Pow(frame.Width, 2.0)));
    
            // Get Keypoints in the whole image
            ExtractSURFKeypoints(ref frame, out all_keypoints);

            List<MKeyPoint> object_keypoints = new List<MKeyPoint>();
            List<MKeyPoint> background_keypoints = new List<MKeyPoint>();
            for (int kpi = 0; kpi < all_keypoints.Length; kpi++)
            {
                MKeyPoint kp = all_keypoints[kpi];
                if (ROI.Contains(kp.Point))
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
            int NumBackgroundKeypoints = background_keypoints.Count;
            num_bgnd_model_keypoints = NumBackgroundKeypoints;
            num_object_model_keypoints = NumObjectKeypoints;

            if (NumObjectKeypoints == 0)
            {
                bValid = false;
            }

            else
            {
                // Getting to here means there is at least one object keypoint in the ROI so the tracker can be initialized
                // The following code is split into two parts based on the keypoint and descriptor extraction method
               
                // Collect information about object keypoints
                ExtractSURFDescriptors(ref frame, object_keypoints.ToArray(), out surf_object_features);

                object_classes = new Matrix<int>(1, NumObjectKeypoints);
                for (int i = 0; i < NumObjectKeypoints; i++) { object_classes[0, i] = i + 1; }

                if (NumBackgroundKeypoints > 0)
                {
                    // Collect information about background keypoints
                    ExtractSURFDescriptors(ref frame, background_keypoints.ToArray(), out surf_background_features);

                    background_classes = new Matrix<int>(1, NumBackgroundKeypoints);
                    //for (int i = 0; i < NumBackgroundKeypoints; i++) { background_classes[0, i] = i + 1; }

                    mInitialBackgroundKeypoints = background_keypoints;
                    mInitialBackgroundFeatures = surf_background_features;

                    mAdaptiveBackgroundKeypointsPrevious = background_keypoints;
                    mAdaptiveBackgroundFeaturesPrevious = surf_background_features;

                    mInitialBackgroundKeypointClasses = new List<int>();

                    for (int ac = 0; ac < background_classes.Cols; ac++)
                    {
                        mInitialBackgroundKeypointClasses.Add(background_classes[0, ac]);
                    }

                    // Stack the features and classes
                    surf_all_features = surf_background_features.ConcateVertical(surf_object_features);
                    all_classes = background_classes.ConcateHorizontal(object_classes);
                }
                else
                {
                    surf_all_features = surf_object_features;
                    all_classes = object_classes;
                }

                mAllInitialFeatures = surf_all_features;
                mActiveFeatures = surf_object_features;

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
                mAllInitialKeypoints = new List<PointF>();
                mAllInitialKeypointClasses = new List<int>();

                mActiveKeypoints = new List<PointF>();
                mActiveKeypointClasses = new List<int>();

                // All and Active Keypoints
                for (int kpi = 0; kpi < all_keypoints.Length; kpi++)
                {
                    MKeyPoint kp = all_keypoints[kpi];
                    if (ROI.Contains(kp.Point))
                    {
                        mActiveKeypoints.Add(kp.Point);
                        mSelectedMKeypoints.Add(kp);
                    }
                    mAllInitialKeypoints.Add(kp.Point);
                }

                mSelectedKeypoints = mActiveKeypoints;
                mSelectedFeatures = mActiveFeatures;
                mSelectedKeypointClasses = mActiveKeypointClasses;

                // Active Keypoint classes
                for (int oc = 0; oc < object_classes.Cols; oc++)
                {
                    mActiveKeypointClasses.Add(object_classes[0, oc]);
                }
                // All Keypoint classes
                for (int ac = 0; ac < all_classes.Cols; ac++)
                {
                    mAllInitialKeypointClasses.Add(all_classes[0, ac]);
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
                mSurfGlobalMatcher = new BruteForceMatcher<float>(DistanceType.L2);
                mSurfGlobalMatcher.Add(mAllInitialFeatures);

                mSurfObjectMatcher = new BruteForceMatcher<float>(DistanceType.L2);
                mSurfObjectMatcher.Add(mActiveFeatures);

                mAdaptiveSurfBackgroundMatcher = new BruteForceMatcher<float>(DistanceType.L2);
                mAdaptiveSurfBackgroundMatcher.Add(mInitialBackgroundFeatures);

                // Set the 'Initialized' Flag
                bInitialized = true;

                // Set the bounding box
                mBoundingBox = new Rectangle((int)ROI.Left, (int)ROI.Top, (int)ROI.Width, (int)ROI.Height);
                bValid = true;

                mVarianceDistance = VarianceDistance(mCenter, ROI);
                #endregion
            }
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
                float[] fb_errX;
                float[] fb_errY;

                ComputeOpticalFlow(prevFrame, curFrame, keyPts, NumKeypoints, out newPts, out bckPts);
                ComputeOpticalFlowError(keyPts, NumKeypoints, bckPts, out fb_errX, out fb_errY);

                // Filter out large tracking errors
                for (int i = 0; i < NumKeypoints; i++)
                {
                    bool large_fb_error = (fb_errX[i] > errThreshold) || (fb_errY[i] > errThreshold);
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
            sw7.Reset();
            int numKP = tracked_keypoints.Length;
            if (numKP > 1)
            {
                //// Added by Matthew Cotter
                //ClusterKeypoints(ref tracked_keypoints, ref tracked_classes);

                EstimateScaleAndRotation(tracked_keypoints, tracked_classes, ref sc_est, ref rot_est);

                sw7.Start();
                centerPt = EstimateCenter(ref tracked_keypoints, ref tracked_classes, sc_est, rot_est);
                sw7.Stop();
            }
            mTrackedKeypoints = new List<PointF>();
            mTrackedKeypoints.AddRange(tracked_keypoints);
        }

        // For new SURF Method
        private void SURFEstimate(PointF[] tracked_keypoints, int[] tracked_classes,
                                     ref PointF centerPt, ref float sc_est, ref float rot_est)
        {
            centerPt = new PointF(float.NaN, float.NaN);
            sc_est = float.NaN;
            rot_est = float.NaN;

            int numKP = tracked_keypoints.Length;
            if (numKP > 1)
            {
                EstimateScaleAndRotation(tracked_keypoints, tracked_classes, ref sc_est, ref rot_est);

                centerPt = SURFEstimateCenter(tracked_keypoints, tracked_classes, sc_est, rot_est);
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
            PointF[] votesArray = votes.ToArray();

            for (int c = 0; c < inliers.Length; c++)
            {
                if (inliers[c])
                {
                    PointF v = votesArray[c];
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

        // For new SURF Method
        private PointF SURFEstimateCenter(PointF[] tracked_keypoints, int[] tracked_classes,
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

            bool[] inliers = Clustering.Clusterizer.GetClusterInliers(votes, mClusterOutlierThreshold);

            float centerX = 0.0F;
            float centerY = 0.0F;
            int inlier_count = 0;
            mInliers = new List<PointF>();
            mOutliers = new List<PointF>();
            List<int> inlier_classes = new List<int>();
            for (int c = 0; c < inliers.Length; c++)
            {
                if (inliers[c])
                {
                    PointF v = votes[c];
                    centerX += v.X;
                    centerY += v.Y;
                    inlier_count++;
                }
                else
                {
                    // Stop tracking outliers
                    mOutliers.Add(tracked_keypoints[c]);
                }
            }
            PointF newCenter = new PointF(centerX / inlier_count, centerY / inlier_count);
            return newCenter;
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
        //<summary>
        //<i>Private method, core implementation.</i> Processes the current frame to track an object which was last detected in the given ROI.
        //</summary>
        //<param name="frame">The current grayscale image frame.</param>
        //<param name="ROI">The rectangle which defines the region in which the target object was last detected.</param>
        //<returns>The estimate of the new ROI off the object to be tracked.</returns>
        private Rectangle ProcessFrame(Image<Gray, Byte> frame, RectangleF ROI)
        {
            //frame = frame.Resize(1000, 563, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            //frame.Save(@"C:\Users\Josh\Desktop\Josh\Research\GrayImages\tide.png");
            // ***************************************************************************************************************************
            // ((( Updated Code ...
            // ***************************************************************************************************************************
            PointF centerEstimate = mCenter;
            float scaleEstimate = mScaleEstimate;
            float rotationEstimate = mRotationEstimate;

            mCurrentFrame = frame;

            if (!Initialized)
            {
                Initialize(frame, ROI);
                return RectFToRect(mBoundingBox);
            }

            PointF[] tracked_keypoints;
            int[] tracked_classes;

            if(mSelectedKeypoints != null & mSelectedKeypoints.Count > 0)
            {
                Track(mPreviousFrame, frame, mActiveKeypoints.ToArray(), mActiveKeypointClasses.ToArray(), mOpticalFlowErrorThreshold, out tracked_keypoints, out tracked_classes);
                Estimate(ref tracked_keypoints, ref tracked_classes, ref centerEstimate, ref scaleEstimate, ref rotationEstimate);
                mIsCenterNaN = float.IsNaN(centerEstimate.X) || float.IsNaN(centerEstimate.Y);

                mCenter = centerEstimate;
                mScaleEstimate = scaleEstimate;
                mRotationEstimate = rotationEstimate;

                ClearVariables();

                // Process frame for new keypoints
                MKeyPoint[] keypoint_vector;
                Matrix<float> keypoint_features;

                //sw1.Restart();
                ExtractSURFKeypoints(ref frame, out keypoint_vector);
                //sw1.Stop();

                total_num_keypoints = keypoint_vector.Length;

                //sw2.Restart();
                ExtractSURFDescriptors(ref frame, keypoint_vector, out keypoint_features);
                //sw2.Stop();

                mCurrentFrameKeypoints = keypoint_vector;
                mCurrentFrameFeatures = keypoint_features;

                FindPotentialObjectKeypoints(frame);

                MatchObjectFeatures();

                AddTrackedKeypoints(tracked_keypoints, tracked_classes);

                mActiveKeypoints = mUpdatedKeypoints;
                mActiveKeypointClasses = mUpdatedKeypointClasses;
                //mActiveFeatures = mUpdatedFeaturesMatrix;

                mPreviousFrame.Dispose();
                mPreviousFrame = mCurrentFrame;

                bValid = false;
                mBoundingBox = new RectangleF();

                // Update state estimate
                if ((!mIsCenterNaN) && (mActiveKeypoints.Count > ((float)mInitialKeypointCount / 10.0)))
                {
                    bValid = true;
                    mPrevCenter = mCenter;

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

                    mBoundingBox = new RectangleF(minX, minY, (maxX - minX), (maxY - minY));
                }

                if (!mIsCenterNaN && !mBoundingBox.IsEmpty)
                {
                    float temp_stddev = VarianceDistance(mCenter, ROI);
                    if (temp_stddev > mVarianceDistance * 1.5)
                    {
                        mVarianceDistance = (float)(mVarianceDistance * 1.5);
                    }
                    else if (temp_stddev < mMinVarianceDistance)
                    {
                        mVarianceDistance = mMinVarianceDistance;
                    }
                    else
                    {
                        mVarianceDistance = temp_stddev;
                    }
                }

                mAdaptiveBackgroundModel = false;
                if (bValid && !mBoundingBox.IsEmpty)
                {
                    mAdaptiveBackgroundModel = true;
                    UpdateAdaptiveBackground();

                    mAdaptiveBackgroundKeypointsPrevious = null;
                    mAdaptiveBackgroundKeypointsPrevious = new List<MKeyPoint>(mAdaptiveBackgroundKeypoints);
                    mAdaptiveBackgroundFeaturesPrevious = null;
                    mAdaptiveBackgroundFeaturesPrevious = mAdaptiveBackgroundFeatures.Clone();
                }
                else
                {
                    mAdaptiveSurfBackgroundMatcher = null;
                    mAdaptiveSurfBackgroundMatcher = new BruteForceMatcher<float>(DistanceType.L2);
                    mAdaptiveSurfBackgroundMatcher.Add(mAdaptiveBackgroundFeaturesPrevious);
                }

                //using (StreamWriter sw = File.AppendText(@"C:\Users\Josh\Desktop\Josh\Research\CMT_Timings\process.txt"))
                //{
                //    sw.WriteLine("{0},{1},{2},{3},{4},{5},{6}", sw1.ElapsedMilliseconds, sw2.ElapsedMilliseconds, sw3.ElapsedMilliseconds, sw4.ElapsedMilliseconds, sw5.ElapsedMilliseconds, sw6.ElapsedMilliseconds, sw7.ElapsedMilliseconds);
                //}

                using (StreamWriter sw = File.AppendText(@"C:\Users\Josh\Desktop\Josh\Research\CMT_Timings\keypoints.txt"))
                {
                    sw.WriteLine("{0},{1},{2},{3}", total_num_keypoints, num_object_model_keypoints, num_bgnd_model_keypoints, num_poss_object_keypoints);
                }

                return RectFToRect(mBoundingBox);
            }

            return new Rectangle();
            // ***************************************************************************************************************************
            // ... Updated Code ))
            // ***************************************************************************************************************************
        }

        private void ClearVariables()
        {
            mPossibleObjectKeypoints.Clear();
            mPossibleObjectFeatures = null;

            mMatchedBackgroundKeypoints.Clear();
            mMatchedBackgroundFeatures.Clear();
            mMatchedBackgroundMatches = null;

            mHomographyBackgroundKeypoints.Clear();
            mHomographyBackgroundFeatures = null;

            mUpdatedKeypoints.Clear();
            mUpdatedKeypointClasses.Clear();
            mUpdatedKeypointFeatures.Clear();
            mUpdatedConfidences.Clear();
            mUpdatedFeaturesMatrix = null;
            mUpdatedKeypointMatchesPoints.Clear();
            mUpdatedMKeypoints.Clear();
            mUpdatedKeypointMatches.Clear();
            mUpdatedDistances.Clear();
        }

        // This function will find which points are a confident match to the background 
        // and which keypoints should be considered as possible matches for the object.
        private void FindPotentialObjectKeypoints(Image<Gray, Byte> frame)
        { 
            FindPossibleBackgroundKeypoints();
            HomographyOfBackgroundKeypoints();
        }

        // This function goes through the first step of background matching.
        // This will provide a list of keypoints and features that are a confident match to the background.
        // However, homography will be done as a second check to make sure the matches are correct.
        private void FindPossibleBackgroundKeypoints()
        {
            int k = 2;
            Matrix<int> bgnd_matches_all;
            Matrix<float> bgnd_matches_all_distances;
            
            bgnd_matches_all = new Matrix<int>(mCurrentFrameFeatures.Rows, k);
            bgnd_matches_all_distances = new Matrix<float>(mCurrentFrameFeatures.Rows, k);

            // Matches all newly extracked features against only the model background features
            //sw3.Restart();
            mAdaptiveSurfBackgroundMatcher.KnnMatch(mCurrentFrameFeatures, bgnd_matches_all, bgnd_matches_all_distances, k, null);
            //sw3.Stop();

            // Go through matches and get rid of features that match to background
            //sw4.Restart();

            MKeyPoint location;
            Matrix<float> features;
            Matrix<int> matches;
            Matrix<float> distances;
            Matrix<float> confidences;
            List<int> matchList;
            List<float> distList;
            List<int> sortedOrder;
            int bestInd;
            int secondBestInd;
            float ratio;
            float confidence;
            //sw9.Reset();
            //sw10.Reset();
            if (mCurrentFrameKeypoints.Length > 0)
            {
                //sw7.Restart();
                for (int skpi = 0; skpi < mCurrentFrameKeypoints.Length; skpi++)
                {
                    //sw9.Start();
                    location = mCurrentFrameKeypoints[skpi];
                    features = mCurrentFrameFeatures.GetRow(skpi);
                    matches = bgnd_matches_all.GetRow(skpi);
                    distances = bgnd_matches_all_distances.GetRow(skpi);
                    confidences = 1 - distances;
                    //sw9.Stop();
                    //sw10.Start();
                    matchList = new List<int>();
                    distList = new List<float>();
                    for (int d = 0; d < matches.Width; d++)
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

                    bestInd = matchList[0];
                    secondBestInd = matchList[1];
                    ratio = (1 - confidences[0, 0]) / (1 - confidences[0, 1]);
                    confidence = confidences[0, 0];
                    //sw10.Stop();
                    
                    // If the match is not a good one, this means that it is probably not a background keypoint
                    // and should be added to the possible object keypoint list for further matching.
                    if ((ratio > mSURFMatchingConfidenceRatio) &&
                        (confidence < mSURFMatchingConfidenceThreshold))
                    {
                        mPossibleObjectKeypoints.Add(location);

                        if (mPossibleObjectFeatures == null || mPossibleObjectFeatures.Rows == 0)
                        {
                            mPossibleObjectFeatures = features;
                        }
                        else
                        {
                            mPossibleObjectFeatures = mPossibleObjectFeatures.ConcateVertical(features);
                        }
                    }
                    // If it is a good match to the background, add it to the background list for further
                    // evaluation using homography
                    else
                    {
                        // Add to adaptive background model
                        mMatchedBackgroundKeypoints.Add(location);
                        mMatchedBackgroundFeatures.Add(features);

                        int[,] new_match = new int[1, 2];
                        new_match[0, 0] = matchList[0];
                        new_match[0, 1] = matchList[1];

                        if (mMatchedBackgroundMatches == null || mMatchedBackgroundMatches.Rows == 0)
                        {
                            mMatchedBackgroundMatches = new Matrix<int>(new_match);
                        }
                        else
                        {
                            mMatchedBackgroundMatches = mMatchedBackgroundMatches.ConcateVertical(new Matrix<int>(new_match));
                        }
                    }
                    
                }
                //sw7.Stop();
            }
        }

        // This function runs the homography algorihtm on all background matches from the 
        // previous to step to make sure they match to the correct keypoints.
        private void HomographyOfBackgroundKeypoints()
        {
            //sw8.Restart();
            // For Debugging Purposes
            List<Tuple<PointF, PointF>> background_matches = new List<Tuple<PointF, PointF>>();

            // Homography Calculation to detect outliers
            Matrix<byte> mask = new Matrix<byte>(mMatchedBackgroundKeypoints.Count, 1);
            mask.SetValue(1);
            VectorOfKeyPoint modelKeypoints = new VectorOfKeyPoint();
            VectorOfKeyPoint observedKeypoints = new VectorOfKeyPoint();

            modelKeypoints.Push((mAdaptiveBackgroundModel) ? mAdaptiveBackgroundKeypoints.ToArray() : mAdaptiveBackgroundKeypointsPrevious.ToArray());
            observedKeypoints.Push(mMatchedBackgroundKeypoints.ToArray());

            HomographyMatrix homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeypoints, observedKeypoints, mMatchedBackgroundMatches, mask, 2);

            for (int mask_index = 0; mask_index < mask.Rows; mask_index++)
            {
                // This keypoint is considered an outlier to the background and will be added to the possible object keypoints
                if (mask[mask_index, 0] == 0)
                {
                    mPossibleObjectKeypoints.Add(mMatchedBackgroundKeypoints[mask_index]);

                    if (mPossibleObjectFeatures == null || mPossibleObjectFeatures.Rows == 0)
                    {
                        mPossibleObjectFeatures = mMatchedBackgroundFeatures[mask_index];
                    }
                    else
                    {
                        mPossibleObjectFeatures = mPossibleObjectFeatures.ConcateVertical(mMatchedBackgroundFeatures[mask_index]);
                    }
                    
                }
                // It is an inlier to the background, so it should be kept as a background keypoint
                else
                {
                    mHomographyBackgroundKeypoints.Add(mMatchedBackgroundKeypoints[mask_index]);

                    if (mHomographyBackgroundFeatures == null || mHomographyBackgroundFeatures.Rows == 0)
                    {
                        mHomographyBackgroundFeatures = mMatchedBackgroundFeatures[mask_index];
                    }
                    else
                    {
                        mHomographyBackgroundFeatures = mHomographyBackgroundFeatures.ConcateVertical(mMatchedBackgroundFeatures[mask_index]);
                    }

                    //int bestInd = mMatchedBackgroundMatches.GetRow(mask_index)[0, 0];

                    // For Debugging Purposes
                    //background_matches.Add(new Tuple<PointF, PointF>(((mAdaptiveBackgroundModel) ? mAdaptiveBackgroundKeypoints[bestInd].Point : mAdaptiveBackgroundKeypointsPrevious[bestInd].Point), mMatchedBackgroundKeypoints[mask_index].Point));
                }
            }
            //sw8.Stop();
            //sw4.Stop();
            // For Debugging Purposes
            //VisualizeMatches(background_matches, mPreviousFrame, mCurrentFrame, "background_matches");
        }

        // This function matches the remaining points not taken out through background subtraction 
        // to the model object.
        private void MatchObjectFeatures()
        {
            // For Debugging Purposes
            List<Tuple<PointF, PointF>> object_matches = new List<Tuple<PointF, PointF>>();

            int k = 2;
            Matrix<int> matches_all;
            Matrix<float> matches_all_distances;

            if (mPossibleObjectFeatures != null && mPossibleObjectFeatures.Rows > 0)
            {
                matches_all = new Matrix<int>(mPossibleObjectFeatures.Rows, k);
                matches_all_distances = new Matrix<float>(mPossibleObjectFeatures.Rows, k);

                num_poss_object_keypoints = mPossibleObjectFeatures.Rows;

                //sw5.Restart();
                mSurfObjectMatcher.KnnMatch(mPossibleObjectFeatures, matches_all, matches_all_distances, k, null);
                //sw5.Stop();

                List<PointF> transformedSprings = Transform(mSprings, mScaleEstimate, -mRotationEstimate);

                //sw6.Restart();
                for (int kpvi = 0; kpvi < mPossibleObjectKeypoints.Count; kpvi++)
                {
                    MKeyPoint location = mPossibleObjectKeypoints[kpvi];
                    Matrix<float> features = mPossibleObjectFeatures.GetRow(kpvi);
                    Matrix<int> matches = matches_all.GetRow(kpvi);
                    Matrix<float> distances = matches_all_distances.GetRow(kpvi);
                    Matrix<float> confidences = 1 - (distances / mMatchingConfidenceScaleFactor);

                    List<int> matchList = new List<int>();
                    List<float> distList = new List<float>();
                    List<int> sortedOrder;

                    for (int d = 0; d < matches.Width; d++)
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

                    int bestInd = matchList[0];
                    int secondBestInd = matchList[1];
                    float ratio = (1 - confidences[0, 0]) / (1 - confidences[0, 1]);
                    int keypoint_class;
                    if(bestInd == -1)
                    {
                        keypoint_class = -1;
                    }
                    else
                    {
                        keypoint_class = mSelectedKeypointClasses[bestInd];
                    }
                    
                    float confidence = confidences[0, 0];

                    if (!mIsCenterNaN)
                    {
                        float distanceFromCenter = L2Norm(mCenter, location.Point);
                        float gaussianCoef = GaussianDistFunction(distanceFromCenter);
                        //confidence *= ((mWeightedMaxDistance - distanceFromCenter) / mWeightedMaxDistance);
                        confidence *= gaussianCoef;
                    }

                    if ((ratio < mSURFMatchingConfidenceRatio) &&
                        (confidence > (!mIsCenterNaN ? mWeightedDistanceConfThreshold : mSURFMatchingConfidenceThreshold)) &&
                        (keypoint_class != -1))
                    {
                        if (mUpdatedKeypointClasses.Contains(keypoint_class))
                        {
                            int index = mUpdatedKeypointClasses.IndexOf(keypoint_class);
                            if (index < mUpdatedKeypointClasses.Count && mUpdatedConfidences[index] < confidence)
                            {
                                if (mUpdatedKeypointClasses.Contains(keypoint_class))
                                {
                                    index = mUpdatedKeypointClasses.IndexOf(keypoint_class);
                                    mUpdatedKeypointClasses.RemoveAt(index);
                                    mUpdatedKeypoints.RemoveAt(index);
                                    mUpdatedMKeypoints.RemoveAt(index);
                                    mUpdatedKeypointFeatures.RemoveAt(index);
                                    mUpdatedConfidences.RemoveAt(index);
                                    mUpdatedKeypointMatchesPoints.RemoveAt(index);
                                    mUpdatedKeypointMatches.RemoveAt(index);
                                    mUpdatedDistances.RemoveAt(index);
                                }
                                mUpdatedKeypoints.Add(location.Point);
                                mUpdatedMKeypoints.Add(location);
                                mUpdatedKeypointClasses.Add(keypoint_class);
                                mUpdatedKeypointFeatures.Add(features);
                                mUpdatedConfidences.Add(confidence);
                                mUpdatedKeypointMatchesPoints.Add(mSelectedKeypoints[bestInd]);
                                mUpdatedKeypointMatches.Add(matches);
                                mUpdatedDistances.Add(distances);
                            }
                        }
                        else
                        {
                            mUpdatedKeypoints.Add(location.Point);
                            mUpdatedMKeypoints.Add(location);
                            mUpdatedKeypointClasses.Add(keypoint_class);
                            mUpdatedKeypointFeatures.Add(features);
                            mUpdatedConfidences.Add(confidence);
                            mUpdatedKeypointMatchesPoints.Add(mSelectedKeypoints[bestInd]);
                            mUpdatedKeypointMatches.Add(matches);
                            mUpdatedDistances.Add(distances);
                        }
                    }

                    if (!mIsCenterNaN)
                    {
                        matches = matches_all.GetRow(kpvi);
                        distances = matches_all_distances.GetRow(kpvi);
                        matchList = new List<int>();
                        distList = new List<float>();
                        for (int d = 0; d < matches.Width; d++)
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

                        PointF relative_location = new PointF(location.Point.X - (!mIsCenterNaN ? mCenter.X : mPrevCenter.X),
                                                              location.Point.Y - (!mIsCenterNaN ? mCenter.Y : mPrevCenter.Y));
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
                        if ((ratio < mSURFMatchingConfidenceRatio) &&
                            (confidence > mSURFMatchingConfidenceThreshold))
                        {
                            if (mUpdatedKeypointClasses.Contains(keypoint_class))
                            {
                                int index = mUpdatedKeypointClasses.IndexOf(keypoint_class);
                                if (mUpdatedConfidences[index] < confidence)
                                {
                                    while (mUpdatedKeypointClasses.Contains(keypoint_class))
                                    {
                                        index = mUpdatedKeypointClasses.IndexOf(keypoint_class);
                                        mUpdatedKeypointClasses.RemoveAt(index);
                                        mUpdatedKeypoints.RemoveAt(index);
                                        mUpdatedMKeypoints.RemoveAt(index);
                                        mUpdatedKeypointFeatures.RemoveAt(index);
                                        mUpdatedConfidences.RemoveAt(index);
                                        mUpdatedKeypointMatchesPoints.RemoveAt(index);
                                        mUpdatedKeypointMatches.RemoveAt(index);
                                        mUpdatedDistances.RemoveAt(index);
                                    }
                                    mUpdatedKeypoints.Add(location.Point);
                                    mUpdatedMKeypoints.Add(location);
                                    mUpdatedKeypointClasses.Add(keypoint_class);
                                    mUpdatedKeypointFeatures.Add(features);
                                    mUpdatedConfidences.Add(confidence);
                                    mUpdatedKeypointMatchesPoints.Add(mSelectedKeypoints[bestInd]);
                                    mUpdatedKeypointMatches.Add(matches);
                                    mUpdatedDistances.Add(distances);
                                }
                            }
                            else
                            {
                                mUpdatedKeypoints.Add(location.Point);
                                mUpdatedMKeypoints.Add(location);
                                mUpdatedKeypointClasses.Add(keypoint_class);
                                mUpdatedKeypointFeatures.Add(features);
                                mUpdatedConfidences.Add(confidence);
                                mUpdatedKeypointMatchesPoints.Add(mSelectedKeypoints[bestInd]);
                                mUpdatedKeypointMatches.Add(matches);
                                mUpdatedDistances.Add(distances);
                            }
                        }
                    }
                }

                Matrix<float> temp_distances = null;

                foreach (Matrix<float> d in mUpdatedDistances)
                {
                    if(temp_distances == null || temp_distances.Rows == 0)
                    {
                        temp_distances = d;
                    }
                    else
                    {
                        temp_distances = temp_distances.ConcateVertical(d);
                    }
                }

                List<PointF> temp_kpts = new List<PointF>();
                Matrix<float> temp_feats = null;
                List<int> temp_classes = new List<int>();

                // Homography Calculation to detect outliers
                Matrix<byte> mask = new Matrix<byte>(mUpdatedKeypoints.Count, 1);
                mask.SetValue(1);
                VectorOfKeyPoint modelKeypoints = new VectorOfKeyPoint();
                VectorOfKeyPoint observedKeypoints = new VectorOfKeyPoint();
                
                modelKeypoints.Push(mSelectedMKeypoints.ToArray());
                observedKeypoints.Push(mUpdatedMKeypoints.ToArray());

                Matrix<int> temp_matches = null;
                if (mUpdatedKeypointMatches != null)
                {
                    for (int mi = 0; mi < mUpdatedKeypointMatches.Count; mi++)
                    {
                        if (temp_matches == null)
                        {
                            temp_matches = mUpdatedKeypointMatches[mi];
                        }
                        else
                        {
                            temp_matches = temp_matches.ConcateVertical(mUpdatedKeypointMatches[mi]);
                        }
                    }
                }

                if (temp_matches != null && temp_matches.Rows > 0)
                {
                    //Features2DToolbox.VoteForUniqueness(temp_distances, 0.8, mask);
                    //Features2DToolbox.VoteForSizeAndOrientation(modelKeypoints, observedKeypoints, temp_matches, mask, 1.5, 20);
                    HomographyMatrix homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeypoints, observedKeypoints, temp_matches, mask, 2);

                    for (int mask_index = 0; mask_index < mask.Rows; mask_index++)
                    {
                        // This keypoint is considered an outlier to the object and will be added to the background keypoints
                        if (mask[mask_index, 0] == 0)
                        {
                            mHomographyBackgroundKeypoints.Add(mUpdatedMKeypoints[mask_index]);

                            if (mHomographyBackgroundFeatures == null || mHomographyBackgroundFeatures.Rows == 0)
                            {
                                mHomographyBackgroundFeatures = mUpdatedKeypointFeatures[mask_index];
                            }
                            else
                            {
                                mHomographyBackgroundFeatures = mHomographyBackgroundFeatures.ConcateVertical(mUpdatedKeypointFeatures[mask_index]);
                            }

                        }
                        // It is an inlier to the object, so it should be kept as a object keypoint
                        else
                        {
                            temp_kpts.Add(mUpdatedKeypoints[mask_index]);
                            temp_classes.Add(mUpdatedKeypointClasses[mask_index]);

                            if (temp_feats == null || temp_feats.Rows == 0)
                            {
                                temp_feats = mUpdatedKeypointFeatures[mask_index];
                            }
                            else
                            {
                                temp_feats = temp_feats.ConcateVertical(mUpdatedKeypointFeatures[mask_index]);
                            }

                            //object_matches.Add(new Tuple<PointF, PointF>(mUpdatedKeypointMatchesPoints[mask_index], mUpdatedKeypoints[mask_index]));
                        }
                    }
                }
                //sw6.Stop();

                if (mUpdatedKeypoints.Count > 0)
                {
                    for (int mi = 0; mi < mUpdatedKeypoints.Count; mi++)
                    {
                        object_matches.Add(new Tuple<PointF, PointF>(mUpdatedKeypointMatchesPoints[mi], mUpdatedKeypoints[mi]));
                    }
                }

                //VisualizeMatches(object_matches, mOriginalImage, mCurrentFrame, "object_matches");

                if(temp_feats != null && temp_feats.Rows > 0)
                {
                    mUpdatedFeaturesMatrix = temp_feats.Clone();
                    mUpdatedKeypoints.Clear();
                    mUpdatedKeypointClasses.Clear();

                    for (int p = 0; p < temp_kpts.Count; p++)
                    {
                        mUpdatedKeypoints.Add(temp_kpts[p]);
                        mUpdatedKeypointClasses.Add(temp_classes[p]);
                    }
                }
            }
        }

        // This function adds all tracked but unmatched keypoints to the active keypoint list
        private void AddTrackedKeypoints(PointF[] tracked_keypoints, int[] tracked_classes)
        {
            for (int tkp = 0; tkp < tracked_keypoints.Length; tkp++)
            {
                int keypoint_class = tracked_classes[tkp];
                if (mUpdatedKeypointClasses != null && !mUpdatedKeypointClasses.Contains(keypoint_class))
                {
                    PointF location = tracked_keypoints[tkp];
                    Matrix<float> features = mActiveFeatures.GetRow(tkp);

                    mUpdatedKeypoints.Add(location);
                    mUpdatedKeypointClasses.Add(keypoint_class);
                    mUpdatedKeypointFeatures.Add(features);
                }
            }
        }        

        // This function adds all keypoints outside of the tracked ROI to the background list
        private void UpdateAdaptiveBackground()
        {
            RectangleF newBB = new RectangleF((int)(mBoundingBox.X - 0.3 * mBoundingBox.X), (int)(mBoundingBox.Y - 0.3 * mBoundingBox.Y), (int)(1.6 * mBoundingBox.Width), (int)(1.6 * mBoundingBox.Y));

            mAdaptiveBackgroundKeypoints = null;
            mAdaptiveBackgroundKeypoints = new List<MKeyPoint>(mHomographyBackgroundKeypoints);
            mAdaptiveBackgroundFeatures = null;
            mAdaptiveBackgroundFeatures = mHomographyBackgroundFeatures.Clone();

            // Update adaptive background model
            if(mPossibleObjectKeypoints != null)
            {
                for (int i = 0; i <  mPossibleObjectKeypoints.Count; i++)
                {
                    MKeyPoint keypt = mPossibleObjectKeypoints[i];
                    Matrix<float> feature = mPossibleObjectFeatures.GetRow(i);

                    // Add all keypoints not in the estimated ROI to the adaptive background model
                    if (!newBB.Contains(keypt.Point))
                    {
                        mAdaptiveBackgroundKeypoints.Add(keypt);

                        if (mAdaptiveBackgroundFeatures == null || mAdaptiveBackgroundFeatures.Rows == 0)
                        {
                            mAdaptiveBackgroundFeatures = feature;
                        }
                        else
                        {
                            mAdaptiveBackgroundFeatures = mAdaptiveBackgroundFeatures.ConcateVertical(feature);
                        }
                    }
                }
            }

            if (mAdaptiveBackgroundKeypoints.Count > 0)
            {
                num_bgnd_model_keypoints = mAdaptiveBackgroundKeypoints.Count;
                mAdaptiveBackgroundModel = true;
                mAdaptiveSurfBackgroundMatcher = null;
                mAdaptiveSurfBackgroundMatcher = new BruteForceMatcher<float>(DistanceType.L2);
                mAdaptiveSurfBackgroundMatcher.Add(mAdaptiveBackgroundFeatures);
            }
        }
        #endregion

        #region Static Methods
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
            double d12 = delta1 * delta1;
            double d22 = delta2 * delta2;
            //float dist = FastSqrt.Sqrt2((float)(d12 + d22));
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
            for (int i = 0; i < order.Count; i++)
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

        private void VisualizeMatches(List<Tuple<PointF, PointF>> match_points, Image<Gray, Byte> img1, Image<Gray, Byte> img2, string saveOption)
        {
            Image<Gray, Byte> img1_clone = img1.Clone();
            Image<Gray, Byte> img2_clone = img2.Clone();
            Image<Gray, Byte> displayImage = img1_clone.ConcateHorizontal(img2_clone);

            foreach (Tuple<PointF, PointF> m in match_points)
            {
                PointF pt2 = new PointF(m.Item2.X + img1_clone.Width, m.Item2.Y);
                displayImage.Draw(new LineSegment2DF(m.Item1, pt2), new Gray(255), 1);
            }

            //string image_path = string.Format("C:\\Users\\jss5451\\Desktop\\CMT_matches\\{0}.jpg", frame_count++.ToString("00000000"));
            string image_path = string.Format("C:\\Users\\Josh\\Desktop\\CMT_matches\\{0}\\{1}.jpg", saveOption, frame_count.ToString("00000000"));
            displayImage.Save(image_path);

            if(saveOption == "object_matches")
            {
                frame_count++;
            }
        }

        private float VarianceDistance(PointF center, RectangleF roi)
        {
            float[] c_distances = new float[4];
            c_distances[0] = L2Norm(center, new PointF(roi.Left, roi.Top));
            c_distances[1] = L2Norm(center, new PointF(roi.Right, roi.Top));
            c_distances[2] = L2Norm(center, new PointF(roi.Right, roi.Bottom));
            c_distances[3] = L2Norm(center, new PointF(roi.Left, roi.Bottom));

            float max_dist = c_distances[0];
            for (int i = 1; i < 4; i++)
            {
                if (c_distances[i] > max_dist)
                {
                    max_dist = c_distances[i];
                }
            }
            return max_dist;
        }

        

        private float GaussianDistFunction(float distance)
        {
            //float dist_sub_mean = distance - mMeanDistance;
            float dist_sqr = distance * distance;
            float exp_value = (-dist_sqr / (2 * mVarianceDistance));

            return (float)Math.Exp(exp_value);
        }
    }
}
