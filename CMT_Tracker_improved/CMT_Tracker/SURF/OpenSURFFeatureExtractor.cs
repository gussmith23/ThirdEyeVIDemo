
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using OpenSURF;
using System.Drawing;

namespace OpenSURF
{
    public class OpenSURFDetector
    {
        private double m_HessianThresh;

        private int m_VectorLength;

        /// <summary>
        /// Hessian Threshold
        /// </summary>
        public double HessianThresh
        {
            get { return m_HessianThresh; }
            set { m_HessianThresh = value; }
        }
        
        /// <summary>
        /// Vector Length (true => 128 elements, false => 64 elements)
        /// </summary>
        public int VectorLength
        {
            get { return m_VectorLength; }
            set { m_VectorLength = value; }
        }

        /// <summary>
        /// Parameter-less constructor.  Sets default values.
        /// </summary>
        public OpenSURFDetector()
        {
            m_HessianThresh = 500;
            m_VectorLength = 64;
        }

        public OpenSURFDetector(double hessianThreshold, bool extended)
        {
            m_HessianThresh = hessianThreshold;
            m_VectorLength = extended ? 128 : 64;
        }
        

        /// <summary>
        /// Computes the SURF KeyPoints
        /// </summary>
        /// <param name="frame">Input Channels</param>
        /// <returns>Key Features</returns>
        public MKeyPoint[] DetectKeyPoints(Image<Gray, byte> frame, Image<Gray, byte> mask)
        {
            OpenSURFIntegralImage iimg = OpenSURFIntegralImage.FromImage(frame.Bitmap);

            // Extract the interest points
            //ipts = FastHessian.getIpoints(0.0005f, 3, 1, iimg);
            //ipts = FastHessian.getIpoints(0.003f, 3, 1, iimg);
            List<OpenSURFIPoint> ipts = OpenSURFFastHessian.getIpoints((float)m_HessianThresh, 3, 1, iimg);

            int NumKeypoints = ipts.Count;

            MKeyPoint[] MKeyPoints = new MKeyPoint[NumKeypoints];

            for (int i = 0; i < NumKeypoints; i++)
            {
                OpenSURFIPoint currentIpoint = ipts[i];
                MKeyPoints[i] = new MKeyPoint();
                MKeyPoints[i].Point = new PointF(currentIpoint.x, currentIpoint.y);
                MKeyPoints[i].Angle = currentIpoint.orientation;
                MKeyPoints[i].Response = currentIpoint.response;
                MKeyPoints[i].Octave = currentIpoint.interval;
                MKeyPoints[i].Size = currentIpoint.scale;
                MKeyPoints[i].ClassId = currentIpoint.laplacian;
            }

            return MKeyPoints;
        }

        public Matrix<float> ComputeDescriptorsRaw(Image<Gray, byte> frame, Image<Gray,byte> mask, VectorOfKeyPoint keypoints)
        {
            OpenSURFIntegralImage iimg = OpenSURFIntegralImage.FromImage(frame.Bitmap);
            List<OpenSURFIPoint> ipts = new List<OpenSURFIPoint>();

            for (int i = 0; i < keypoints.Size; i++)
            {
                MKeyPoint kp = keypoints[i];
                OpenSURFIPoint CurrentIpoint = new OpenSURFIPoint();
                CurrentIpoint.x = kp.Point.X;
                CurrentIpoint.y = kp.Point.Y;
                CurrentIpoint.orientation = kp.Angle;
                CurrentIpoint.response = kp.Response;
                CurrentIpoint.interval = kp.Octave;
                CurrentIpoint.scale = kp.Size;
                CurrentIpoint.laplacian = kp.ClassId;
                ipts.Add(CurrentIpoint);
            }

            // Describe the interest points
            OpenSURFDescriptor.DecribeInterestPoints(ipts, false, false, iimg);   

            int NumKeypoints = ipts.Count;
            Matrix<float> KeyDescriptors = new Matrix<float>(NumKeypoints, 64, 1);

            for (int i = 0; i < NumKeypoints; i++)
            {
                for (int c = 0; c < 64; c++)
                    KeyDescriptors.Data[i, c] = ipts[i].descriptor[c];
            }

            return KeyDescriptors;
        }
    }
}
