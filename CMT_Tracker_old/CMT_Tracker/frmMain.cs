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
        private static SURFDetector SURF = new SURFDetector(400, false);

        private bool outputBB = true;
        private bool isInit = false;
        private bool isTracking = false;
        private StreamWriter outputFile;

        private bool display = true;

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

        private static void ExternalSURFKeypoints(ref Image<Gray, Byte> frame, out MKeyPoint[] all_keypoints)
        {
            Console.WriteLine("External SURF Keypoint Detector");
            all_keypoints = SURF.DetectKeyPoints(frame, null);
        }
        private static void ExternalSURFDescriptors(ref Image<Gray, Byte> frame, MKeyPoint[] keypoints, out Matrix<float> features)
        {
            Console.WriteLine("External SURF Keypoint Descriptor Extractor");
            Emgu.CV.Util.VectorOfKeyPoint keypoint_vector = new Emgu.CV.Util.VectorOfKeyPoint();
            keypoint_vector.Push(keypoints);
            features = SURF.ComputeDescriptorsRaw(frame, null, keypoint_vector);
        }
        #endregion

        private string sourcePath; // = @"C:\Users\Josh\Desktop\Josh\Research\VOT_Toolkit_Videos\ball";

        public frmTrackMain()
        {
            //Console.WriteLine("STARTING CMT");

            if(display)
            {
                InitializeComponent();
                File.Create(@"C:\Users\Josh\Desktop\Josh\Research\CMT_Timings\totals.txt");
                File.Create(@"C:\Users\Josh\Desktop\Josh\Research\CMT_Timings\process.txt");
                File.Create(@"C:\Users\Josh\Desktop\Josh\Research\CMT_Timings\keypoints.txt");
            }

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

            //overlay.MouseDown += overlay_MouseDown;

            // Start Tracking Code for MatLab Toolkit
            //isTracking = true;
            //while(isTracking)
            //{
                tracking();
            //}
        }

        // This function is to be used for MatLab VOT Toolkit
        // It is for automatic evaluation purposes
        // Added by JSS 12/10/2014
        private void tracking()
        {
            if (!isInit)
            {
                bool initSuccess = false;

                // Initialize with text files
                initSuccess = InitializeTracker();
                
                if (!initSuccess)
                {
                    Console.WriteLine("Unable to initialiaze tracker.");
                    Console.WriteLine("Exiting program.");
                    isTracking = false;
                    outputFile.Close();
                    Application.Exit();
                }
                else
                {
                    isInit = true;
                    //Console.WriteLine("Starting Capture Timer");
                    mCaptureTimer.Start();
                }
            } 

            else
            {
                mCaptureTimer.Start();
            }

        }

        private bool InitializeTracker()
        {
            //Console.WriteLine("Initializing Tracker");

            bool ableToInitialize = false;

            //Console.WriteLine("Stop Capture Timer");
            mCaptureTimer.Stop();
            mTracker.Reset();
            mROIMode = true;

            //Console.WriteLine("Initializing Tracker");

            // Open Output File
            outputFile = new StreamWriter("output.txt");

            // Read images.txt file to get path of starting image
            StreamReader imageFile = new StreamReader("images.txt");
            string line;

            if ((line = imageFile.ReadLine()) != null)
            {
                // Get source path
                int pos = line.LastIndexOf('\\');
                sourcePath = line.Substring(0, pos);

                // Get starting frame number
                pos = line.LastIndexOf('.');
                mNextFrame = Convert.ToInt32(line.Substring(pos - 4, 4));

                // FOR DEBUGGING ONLY
                //mNextFrame = 19;

                ableToInitialize = true;
            }
            imageFile.Close();

            // For Testing Purposes on laptop
            //sourcePath = @"C:\Users\Josh\Desktop\Josh\Research\VOT_Toolkit_Videos\woman";
            // For Testing purposes on Lab computer
            //sourcePath = @"C:\vot-toolkit-master\vot-workspace\sequences\ball";
            sourcePath = @"C:\Users\Josh\Desktop\Josh\Research\HD_Videos\DeckofCards";

            CaptureNextFrame();

            // Read region.txt file to get the initial bounding box
            StreamReader regionFile = new StreamReader("region.txt");

            if ((line = regionFile.ReadLine()) != null)
            {
                string[] values = line.Split(',');
                float[] x = new float[4];
                float[] y = new float[4];

                for (int i = 0; i < 8; i++)
                {
                    if (i % 2 == 0)
                    {
                        x[i / 2] = float.Parse(values[i]);
                    }
                    else
                    {
                        y[i / 2] = float.Parse(values[i]);
                    }
                }

                float maxX = 0;
                float minX = 4000;
                float maxY = 0;
                float minY = 4000;
                
                for (int i = 0; i < 4; i++)
                {
                    maxX = (x[i] > maxX) ? ((x[i] > 0) ? x[i] : 0) : maxX;
                    minX = (x[i] < minX) ? ((x[i] > 0) ? x[i] : 0) : minX;
                    maxY = (y[i] > maxY) ? ((y[i] > 0) ? y[i] : 0) : maxY;
                    minY = (y[i] < minY) ? ((y[i] > 0) ? y[i] : 0) : minY;
                }

                maxX = (maxX > trackFrame.Width) ? trackFrame.Width : maxX;
                minX = (minX < 0) ? 0 : minX;
                maxY = (maxY > trackFrame.Height) ? trackFrame.Height : maxY;
                minY = (minY < 0) ? 0 : minY;

                int initWidth = ((maxX - minX) > 0) ? (int)(maxX - minX) : 0;
                int initHeight = ((maxY - minY) > 0) ? (int)(maxY - minY) : 0;

                initWidth = (initWidth + (int)minX > trackFrame.Width) ? trackFrame.Width - (int)minX : initWidth;
                initHeight = (initHeight + (int)minY > trackFrame.Height) ? trackFrame.Height - (int)minY : initHeight;

                mDefinedROI = new Rectangle((int)minX, (int)minY, initWidth, initHeight);

                // FOR DEBUGGING ONLY
                mDefinedROI = new Rectangle(621,463,181,234);

                outputFile.WriteLine("{0},{1},{2},{3}", mDefinedROI.X, mDefinedROI.Y, mDefinedROI.Width, mDefinedROI.Height);
                
            }

            if (ableToInitialize)
            {
                // Initialize Tracker
                mTracker.Initialize(trackFrame, mDefinedROI);

                // Draw Initial Frame
                //trackFrame.Draw(mDefinedROI, new Bgr(255, 0, 0), 2);
                //for (int i = 0; i < mTracker.ActiveKeypointsF.Count; i++)
                //{
                //    trackFrame.Draw(new CircleF(mTracker.ActiveKeypointsF[i], 3), new Bgr(Color.Blue), 1);   
                //}
                ////for (int i = 0; i < mTracker.InitialBackgroundKeypoints.Count; i++)
                ////{
                ////    trackFrame.Draw(new CircleF(mTracker.InitialBackgroundKeypoints[i].Point, 3), new Bgr(Color.Red), 1);   
                ////}

                //trackFrame.Draw(new CircleF(mTracker.CenterF, 4), new Bgr(Color.Green), -1);   // Green

                //trackFrame.Save("C:\\Users\\Josh\\Desktop\\init_image.jpg");

                mROIMode = false;
            }

            return ableToInitialize;
        }

        void mCaptureTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (CaptureNextFrame() == 0)
            {
                //Console.WriteLine("Stop Capture Timer");
                mCaptureTimer.Stop();
                outputFile.Close();
                Environment.Exit(-1);
            }
            else
            {
                //Console.WriteLine("Start Capture Timer");
                mCaptureTimer.Start();
            }
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

        private static int mNextFrame; // = 1;

        private int CaptureNextFrame()
        {
            string nextFrame = String.Format("{0}\\{1}.jpg", sourcePath, mNextFrame++.ToString("00000000"));
            //Console.WriteLine(nextFrame);
            if (!File.Exists(nextFrame))
            {
                nextFrame = String.Format("{0}\\{1}.png", sourcePath, mNextFrame++.ToString("00000000"));
            }
            if (File.Exists(nextFrame))
            {
                Image<Bgr, Byte> frame = new Image<Bgr, byte>(nextFrame);
                //frame = frame.Resize(1000, 563, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                status.Text = string.Format("{0}", mNextFrame - 1);
                if (!mROIMode)
                {
                    Stopwatch sw = new Stopwatch();

                    sw.Start();
                    mDefinedROI = mTracker.ProcessFrame(frame, mDefinedROI);
                    sw.Stop();
                    using (StreamWriter stream = File.AppendText(@"C:\Users\Josh\Desktop\Josh\Research\CMT_Timings\totals.txt"))
                    {
                        stream.WriteLine("{0}", sw.ElapsedMilliseconds);
                    }
                    //Console.WriteLine("Frame processed in: {0}", sw.Elapsed);
                    if (mTracker.Valid)
                    {
                        if(display)
                        {
                            frame.Draw(mDefinedROI, new Bgr(255, 0, 0), 2);
                            // Draw points that weren't matched to background (for Debugging purposes)
                            //for (int i = 0; i < mTracker.NonBackgroundPoints.Count; i++)
                            //{
                            //    frame.Draw(new CircleF(mTracker.NonBackgroundPoints[i].Point, 2), new Bgr(0, 102, 255), 1);   // Orange
                            //}
                            for (int i = 0; i < mTracker.TrackedKeypoints.Count; i++)
                            {
                                frame.Draw(new CircleF(mTracker.ActiveKeypointsF[i], 3), new Bgr(255, 255, 255), 1);   // White
                            }
                            for (int i = 0; i < mTracker.Inliers.Count; i++)
                            {
                                frame.Draw(new CircleF(mTracker.Inliers[i], 3), new Bgr(255, 0, 0), 1);   // Blue
                            }
                            for (int i = 0; i < mTracker.Outliers.Count; i++)
                            {
                                frame.Draw(new CircleF(mTracker.Outliers[i], 3), new Bgr(0, 0, 255), 1);   // Red
                            }
                            frame.Draw(new CircleF(mTracker.CenterF, 4), new Bgr(Color.Green), -1);   // Green
                            //frame.Save(String.Format(@"C:\Users\Josh\Desktop\Josh\Research\TrackingImages\BackgroundSub\{0}.jpg", (mNextFrame - 1).ToString("00000000")));
                        }

                        if(outputBB)
                        {
                            outputFile.WriteLine("{0},{1},{2},{3}", mDefinedROI.X, mDefinedROI.Y, mDefinedROI.Width, mDefinedROI.Height);
                        }
                    }
                    else if (!mTracker.Valid || outputBB)
                    {
                        //frame.Save(String.Format(@"C:\Users\Josh\Desktop\Josh\Research\TrackingImages\BackgroundSub\{0}.jpg", (mNextFrame - 1).ToString("00000000")));
                        outputFile.WriteLine("nan,nan,nan,nan");
                    }

                }
                if(display)
                {
                    overlay.Image = frame.ToBitmap();
                }
                
                if (trackFrame != null) trackFrame.Dispose();
                trackFrame = frame;

                return 1;
            }
            else
            {
                // File does not exist
                return 0;
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
