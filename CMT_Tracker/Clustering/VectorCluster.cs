using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clustering
{
    /// <summary>
    /// Object representing a cluster of vectors in floating-point coordinates.
    /// </summary>
    /// <typeparam name="labelType">The type of the label used for labelling the vectors in the cluster.</typeparam>
    public class VectorCluster<labelType>
    {
        #region Private Members
        private List<labelType> mLabels;
        private List<List<float>> mVectors;
        private List<float> mCenter;
        private int mVectorSize;
        #endregion

        #region Public Properties
        /// <summary>
        /// The list of labels corresponding to each vectors in the cluster.
        /// </summary>
        public List<labelType> Labels { get { return mLabels; } }

        /// <summary>
        /// The list of vectors in the cluster.
        /// </summary>
        public List<List<float>> Vectors { get { return mVectors; } }

        /// <summary>
        /// The center of the cluster, computed as the mean of the vectors in the cluster.
        /// </summary>
        public List<float> Center { get { return mCenter; } }

        /// <summary>
        /// The number of vectors in the cluster.
        /// </summary>
        public int Members { get { return (mVectors == null ? 0 : mVectors.Count); } }

        /// <summary>
        /// The number of data points in each vector.
        /// </summary>
        public int VectorSize { get { return mVectorSize; } }

        /// <summary>
        /// Determines whether a vector with the specified label is a member of the cluster.
        /// </summary>
        /// <param name="label">The label to be tested for membership in the cluster.</param>
        /// <returns>True if any member of the cluster has the given label. False, otherwise.</returns>
        public bool Includes(labelType label) { return (mLabels == null ? false : mLabels.Contains(label)); }
        #endregion

        /// <summary>
        /// Constructor. Creates a cluster containing a single vector with the provided label.
        /// </summary>
        /// <param name="vec">The seed vector for the cluster.</param>
        /// <param name="label">The label associated with the seed vector.</param>
        public VectorCluster(List<float> vec, labelType label)
        {
            mLabels = new List<labelType>();
            mVectors = new List<List<float>>();
            mCenter = new List<float>();
            mVectorSize = vec.Count;
            Add(vec, label);
        }
        /// <summary>
        /// Constructor. Creates a new cluster by merging two clusters.
        /// </summary>
        /// <param name="c1">The first cluster.</param>
        /// <param name="c2">The second cluster.</param>
        public VectorCluster(VectorCluster<labelType> c1, VectorCluster<labelType> c2)
        {
            if (c1.VectorSize != c2.VectorSize)
            {
                throw new Exception(String.Format("Vector size mismatch in new cluster merge. Expected {0}, received {1}", c1.VectorSize, c2.VectorSize));
            }

            mLabels = c1.Labels;
            mVectors = c1.Vectors;
            mCenter = c1.Center;
            mVectorSize = c1.VectorSize;
            Add(c2);
        }

        /// <summary>
        /// Adds another cluster to the current cluster.
        /// </summary>
        /// <param name="other">The other cluster to be added.</param>
        public void Add(VectorCluster<labelType> other)
        {
            if (this.VectorSize != other.VectorSize)
            {
                throw new Exception(String.Format("Vector size mismatch in cluster add [cluster]. Expected {0}, received {1}", this.VectorSize, other.VectorSize));
            }
            for (int i = 0; i < other.Members; i++)
            {
                Add(other.Vectors[i], other.Labels[i]);
            }
        }

        /// <summary>
        /// Adds a new vector to the current cluster, with the given label.
        /// </summary>
        /// <param name="vec">The new vector for the cluster.</param>
        /// <param name="label">The label associated with the new vector.</param>
        public void Add(List<float> vec, labelType label)
        {
            if (this.VectorSize != vec.Count)
            {
                throw new Exception(String.Format("Vector size mismatch in cluster add [vector]. Expected {0}, received {1}", this.VectorSize, vec.Count));
            }

            int currentCount = mVectors.Count;
            int newCount = currentCount + 1;
            for (int i = 0; i < mVectorSize;i++)
            {
                mCenter[i] = ((mCenter[i] * currentCount) + vec[i]) / newCount;
            }

            mVectors.Add(vec);
            mLabels.Add(label);
        }

        /// <summary>
        /// Computes the distance from the center of this cluster to the center of the given cluster.
        /// </summary>
        /// <param name="other">The other cluster to be measured.</param>
        /// <returns>The distance from the center of this cluster to the center of the given cluster.</returns>
        public float DistanceFrom(VectorCluster<labelType> other)
        {
            if (other.VectorSize != this.VectorSize)
            {
                throw new Exception(String.Format("Vector size mismatch in cluster distance comparison. Expected {0}, received {1}", this.VectorSize, other.VectorSize));
            }
            return DistanceBetween(this, other);
        }

        /// <summary>
        /// Computes the distance between the centers of two clusters.
        /// </summary>
        /// <param name="c1">The first cluster.</param>
        /// <param name="c2">The second cluster.</param>
        /// <returns>The distance between the centers of the two clusters.</returns>
        public static float DistanceBetween(VectorCluster<labelType> c1, VectorCluster<labelType> c2)
        {
            if (c1.VectorSize != c2.VectorSize)
            {
                throw new Exception(String.Format("Vector size mismatch in cluster distance comparison. Expected {0}, received {1}", c1.VectorSize, c2.VectorSize));
            }
            float sum = 0.0F;
            for (int i = 0; i < c1.VectorSize; i ++)
            {
                sum += (float)Math.Pow(c2.Center[i] - c1.Center[i], 2);
            }
            return (float)Math.Sqrt(sum);
        }
    }
}
