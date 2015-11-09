using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.Diagnostics;
using Emgu.CV.Features2D;

namespace CMT_Tracker
{
    public partial class frmTrackMain : Form
    {
        private Capture mCap;
        private CMT mTracker;
        private Rectangle mDefinedROI;

        private bool mROIMode;
        private bool mTopLeft;
        private bool mBottomRight;
        private Point pTopLeft;
        private Point pBottomRight;
        private System.Timers.Timer mCaptureTimer;
        private Image<Bgr, Byte> trackFrame;
        private static Brisk featureDetector = new Brisk(30, 3, 1.0f);

        #region External Delegates

        private static void ExternalBRISKKeypoints(ref Image<Gray, Byte> frame, out MKeyPoint[] all_keypoints)
        {
            //Console.WriteLine("External BRISK Keypoint Detector");
            all_keypoints = featureDetector.DetectKeyPoints(frame, null);
        }
        private static void ExternalBRISKDescriptors(ref Image<Gray, Byte> frame, MKeyPoint[] keypoints, out Matrix<byte> features)
        {
            //Console.WriteLine("External BRISK Keypoint Descriptor Extractor");
            Emgu.CV.Util.VectorOfKeyPoint keypoint_vector = new Emgu.CV.Util.VectorOfKeyPoint();
            keypoint_vector.Push(keypoints);
            features = featureDetector.ComputeDescriptorsRaw(frame, null, keypoint_vector);
        }
        private static void ExternalComputeLKOpticalFlow(Image<Gray, Byte> prevFrame, Image<Gray, Byte> curFrame, PointF[] keyPts, int NumKeypoints, out PointF[] newPts, out PointF[] bckPts)
        {
            //Console.WriteLine("External Optical Flow");
            Size opticalFlowWindow = new Size(21, 21);
            MCvTermCriteria opticalFlowCriteria = new MCvTermCriteria(10, 0.03);
            opticalFlowCriteria.type = Emgu.CV.CvEnum.TERMCRIT.CV_TERMCRIT_EPS | Emgu.CV.CvEnum.TERMCRIT.CV_TERMCRIT_ITER;
            int opticalFlowLevel = 2;

            byte[] fwdStatus;
            float[] fwdError;
            byte[] bckStatus;
            float[] bckError;

            OpticalFlow.PyrLK(prevFrame, curFrame,
                keyPts, opticalFlowWindow,
                opticalFlowLevel, opticalFlowCriteria,
                out newPts, out fwdStatus, out fwdError);

            OpticalFlow.PyrLK(curFrame, prevFrame,
                newPts, opticalFlowWindow,
                opticalFlowLevel, opticalFlowCriteria,
                out bckPts, out bckStatus, out bckError);
        }
        #endregion

        private string sourcePath = @"C:\Users\Matt\Desktop\Tracking Algorithms\CMT\cmt_dataset\board";
        public frmTrackMain()
        {
            InitializeComponent();
            FileInfo assembly = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);

            mTracker = new CMT();
            // Connect external delegate functions
            mTracker.ExtractKeypoints = new CMT.ExtractKeypointDelegate(ExternalBRISKKeypoints);
            mTracker.ExtractKeypointDescriptors = new CMT.ExtractKeypointDescriptorDelegate(ExternalBRISKDescriptors);
            mTracker.OpticalFlowFunction = new CMT.ComputeOpticalFlowDelegate(ExternalComputeLKOpticalFlow);

            mTracker.EstimateScale = true;
            mTracker.EstimateRotation = false;

            mROIMode = false;
            mTopLeft = false;
            mBottomRight = false;
            mCaptureTimer = new System.Timers.Timer(33);
            mCaptureTimer.AutoReset = false;
            mCaptureTimer.Elapsed += mCaptureTimer_Elapsed;
            //mCaptureTimer.Start();

            overlay.MouseDown += overlay_MouseDown;
        }

        void mCaptureTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CaptureNextFrame();
            mCaptureTimer.Start();
        }

        void overlay_MouseDown(object sender, MouseEventArgs e)
        {
            if (mROIMode)
            {
                if (mTopLeft)
                {
                    pTopLeft = e.Location;
                    status.Text = "Select the bottom-right corner of the object to track";

                    mTopLeft = false;
                    mBottomRight = true;
                }
                else if (mBottomRight)
                {
                    pBottomRight = e.Location;
                    status.Text = string.Empty;

                    mDefinedROI = new Rectangle(pTopLeft.X, pTopLeft.Y, pBottomRight.X - pTopLeft.X, pBottomRight.Y - pTopLeft.Y);
                    //mDefinedROI = new Rectangle(199, 110, 48, 55);

                    mTracker.Initialize(trackFrame, mDefinedROI);

                    mTopLeft = false;
                    mBottomRight = false;
                    mROIMode = false;
                    mCaptureTimer.Start();
                }
            }
        }

        private static int mNextFrame = 1;

        private void CaptureNextFrame()
        {
            string nextFrame = String.Format("{0}\\{1}.jpg", sourcePath, mNextFrame++.ToString("00000000"));
            if (!File.Exists(nextFrame))
            {
                nextFrame = String.Format("{0}\\{1}.png", sourcePath, mNextFrame++.ToString("00000000"));
            }
            if (File.Exists(nextFrame))
            {
                Image<Bgr, Byte> frame = new Image<Bgr, byte>(nextFrame);
                if (!mROIMode)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    mDefinedROI = mTracker.ProcessFrame(frame, mDefinedROI);
                    sw.Stop();
                    Console.WriteLine("Frame processed in: {0}", sw.Elapsed);
                    if (mTracker.Valid)
                    {
                        frame.Draw(mDefinedROI, new Bgr(255, 0, 0), 2);
                        for (int i = 0; i < mTracker.TrackedKeypoints.Count; i++)
                        {
                            frame.Draw(new CircleF(mTracker.ActiveKeypointsF[i], 3), new Bgr(255, 255, 255), 1);
                        }
                        for (int i = 0; i < mTracker.Inliers.Count; i++)
                        {
                            frame.Draw(new CircleF(mTracker.Inliers[i], 3), new Bgr(255, 0, 0), 1);
                        }
                        for (int i = 0; i < mTracker.Outliers.Count; i++)
                        {
                            frame.Draw(new CircleF(mTracker.Outliers[i], 3), new Bgr(0, 0, 255), 1);
                        }
                        frame.Draw(new CircleF(mTracker.CenterF, 4), new Bgr(Color.Green), -1);
                    }
                }
                overlay.Image = frame.ToBitmap();
                if (trackFrame != null) trackFrame.Dispose();
                trackFrame = frame;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            mNextFrame = 1;
            mCaptureTimer.Stop();
            mTracker.Reset();
            mROIMode = true;
            mTopLeft = true;
            mBottomRight = true;
            status.Text = "Select the top-left corner of the object to track";

            CaptureNextFrame();
        }
    }
}
