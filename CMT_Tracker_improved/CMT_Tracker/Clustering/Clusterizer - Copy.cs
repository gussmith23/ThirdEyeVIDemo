using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Clustering
{
    public static class Clusterizer
    {
        #region PointCluster Methods
        public static bool[] GetClusterInliers(List<PointF> points, float threshold)
        {
            List<int> labels = new List<int>();
            labels.AddRange(Enumerable.Range(0, points.Count));
            return GetClusterInliers(points, labels, threshold);
        }

        public static bool[] GetClusterInliers(List<PointF> points, List<int> labels, float threshold)
        {
            List<Clustering.PointCluster<int>> clusters = Clustering.Clusterizer.CreateClusters<int>(points, labels, threshold);

            var largestCountQuery = clusters.Max((item) => item.Members);
            Clustering.PointCluster<int> largestCluster = null;
            foreach (Clustering.PointCluster<int> c in clusters)
            {
                if (c.Members == largestCountQuery)
                {
                    largestCluster = c;
                    break;
                }
            }

            List<bool> inliers = new List<bool>();
            for (int i = 0; i < points.Count; i++)
            {
                inliers.Add(largestCluster.Includes(labels[i]));
            }
            return inliers.ToArray();
        }

        public static List<PointCluster<T>> CreateClusters<T>(List<PointF> points, List<T> labels, float maxDistance)
        {
            List<PointCluster<T>> initialClusters = new List<PointCluster<T>>();
            PointF[] pointsArray = points.ToArray();
            T[] labelsArray = labels.ToArray();

            for(int i = 0; i < pointsArray.Length; i++)
            {
                initialClusters.Add(new PointCluster<T>(pointsArray[i], labelsArray[i]));
            }

            return Clusterize<T>(initialClusters, maxDistance);
        }

        unsafe public static List<PointCluster<T>> Clusterize<T>(List<PointCluster<T>> clusters, float maxDistance)
        {
            float bestDistance = float.PositiveInfinity;
            Tuple<PointCluster<T>, PointCluster<T>> bestPair = null;
            PointCluster<T>[] theClusters = clusters.ToArray();

            float[] arr = new float[clusters.Count * 2];

            fixed (float* arrFixed = arr)
            {

                for (int i = 0; i < theClusters.Length; i++)
                {
                    arrFixed[i * 2 + 0] = theClusters[i].Center.X;
                    arrFixed[i * 2 + 1] = theClusters[i].Center.Y;
                }
            }

            float[] bestdistarray = new float[clusters.Count];
            int[] bestdistindex = new int[clusters.Count];
            Parallel.For(0, clusters.Count, i => {
                   bestdistarray[i] = PointCluster<float>.DistanceBetween(i, arr, clusters.Count, out bestdistindex[i]);
            });
       

			for (int i = 0; i < bestdistarray.Length; i++)
			{
				if (bestdistarray[i] < bestDistance)
				{

					bestPair = new Tuple<PointCluster<T>, PointCluster<T>>(theClusters[i], theClusters[bestdistindex[i]]);
					bestDistance = bestdistarray[i];
				}
			}
            

            if (bestDistance < maxDistance)
            {
                PointCluster<T> c1 = bestPair.Item1;
                PointCluster<T> c2 = bestPair.Item2;
                clusters.Remove(c1);
                clusters.Remove(c2);
                clusters.Add(new PointCluster<T>(c1, c2));
                if (clusters.Count > 1)
                {
                    return Clusterize<T>(clusters, maxDistance);
                }
            }

            return clusters;
        }
        
        #endregion
    }
}
