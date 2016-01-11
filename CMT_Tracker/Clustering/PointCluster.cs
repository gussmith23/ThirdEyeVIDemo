using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Clustering
{
    /// <summary>
    /// Object representing a cluster of points in floating-point coordinates.
    /// </summary>
    /// <typeparam name="labelType">The type of the label used for labelling the points in the cluster.</typeparam>
    public class PointCluster<labelType>
    {
        #region Private Members
        private List<labelType> mLabels;
        private List<PointF> mPoints;
        private PointF mCenter;
        #endregion

        #region Public Properties
        /// <summary>
        /// The list of labels corresponding to each point in the cluster.
        /// </summary>
        public List<labelType> Labels { get { return mLabels; } }
        
        /// <summary>
        /// The list of points in the cluster.
        /// </summary>
        public List<PointF> Points { get { return mPoints; } }

        /// <summary>
        /// The center of the cluster, computed as the mean of the points in the cluster.
        /// </summary>
        public PointF Center { get { return mCenter; } }

        /// <summary>
        /// The number of points in the cluster.
        /// </summary>
        public int Members { get { return (mPoints == null ? 0 : mPoints.Count); } }

        /// <summary>
        /// Determines whether a point with the specified label is a member of the cluster.
        /// </summary>
        /// <param name="label">The label to be tested for membership in the cluster.</param>
        /// <returns>True if any member of the cluster has the given label. False, otherwise.</returns>
        public bool Includes(labelType label) { return (mLabels == null ? false : mLabels.Contains(label)); }
        #endregion

        /// <summary>
        /// Constructor. Creates a cluster containing a single point with the provided label.
        /// </summary>
        /// <param name="pt">The seed point for the cluster.</param>
        /// <param name="label">The label associated with the seed point.</param>
        public PointCluster(PointF pt, labelType label)
        {
            mLabels = new List<labelType>();
            mPoints = new List<PointF>();
            mCenter = new PointF(0, 0);
            Add(pt, label);
        }
        /// <summary>
        /// Constructor. Creates a new cluster by merging two clusters.
        /// </summary>
        /// <param name="c1">The first cluster.</param>
        /// <param name="c2">The second cluster.</param>
        public PointCluster(PointCluster<labelType> c1, PointCluster<labelType> c2)
        {
            mLabels = c1.Labels;
            mPoints = c1.Points;
            mCenter = c1.Center;
            Add(c2);
        }

        /// <summary>
        /// Adds another cluster to the current cluster.
        /// </summary>
        /// <param name="other">The other cluster to be added.</param>
        public void Add(PointCluster<labelType> other)
        {
            for (int i = 0; i < other.Points.Count; i++)
            {
                Add(other.Points[i], other.Labels[i]);
            }
        }

        /// <summary>
        /// Adds a new point to the current cluster, with the given label.
        /// </summary>
        /// <param name="pt">The new point for the cluster.</param>
        /// <param name="label">The label associated with the new point.</param>
        public void Add(PointF pt, labelType label)
        {
            float centerX = mCenter.X * mPoints.Count;
            float centerY = mCenter.Y * mPoints.Count;

            centerX += pt.X;
            centerY += pt.Y;

            mPoints.Add(pt);
            mLabels.Add(label);

            mCenter = new PointF(centerX / mPoints.Count, centerY / mPoints.Count);
        }

        /// <summary>
        /// Computes the distance from the center of this cluster to the center of the given cluster.
        /// </summary>
        /// <param name="other">The other cluster to be measured.</param>
        /// <returns>The distance from the center of this cluster to the center of the given cluster.</returns>
        public float DistanceFrom(PointCluster<labelType> other)
        {
            return DistanceBetween(this, other);
        }

        /// <summary>
        /// Computes the distance between the centers of two clusters.
        /// </summary>
        /// <param name="c1">The first cluster.</param>
        /// <param name="c2">The second cluster.</param>
        /// <returns>The distance between the centers of the two clusters.</returns>
        public static float DistanceBetween(PointCluster<labelType> c1, PointCluster<labelType> c2)
        {
            float deltaX2 = c1.Center.X - c2.Center.X;
            deltaX2 *= deltaX2;
            float deltaY2 = c1.Center.Y - c2.Center.Y;
            deltaY2 *= deltaY2;
            float dist = (float)Math.Sqrt(deltaX2 + deltaY2);
            //float dist = FastSqrt.Sqrt(deltaX2 + deltaY2);
            return dist;
        }
    }
}
