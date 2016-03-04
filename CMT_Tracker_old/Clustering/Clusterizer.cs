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

        public static List<PointCluster<T>> Clusterize<T>(List<PointCluster<T>> clusters, float maxDistance)
        {
            float bestDistance = float.PositiveInfinity;
            Tuple<PointCluster<T>, PointCluster<T>> bestPair = null;
            PointCluster<T>[] theClusters = clusters.ToArray();

            for(int i = 0; i < clusters.Count; i++)
            {
                PointCluster<T> c1 = theClusters[i];
                for(int j = i + 1; j < clusters.Count; j++)
                {
                    PointCluster<T> c2 = theClusters[j];
                    float dist = c1.DistanceFrom(c2);
                    if (dist < bestDistance)
                    {
                        bestPair = new Tuple<PointCluster<T>, PointCluster<T>>(c1, c2);
                        bestDistance = dist;
                    }
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

        #region VectorCluster Methods
        public static bool[] GetClusterInliers(List<List<float>> points, float threshold)
        {
            List<int> labels = new List<int>();
            labels.AddRange(Enumerable.Range(0, points.Count));
            return GetClusterInliers(points, labels, threshold);
        }

        public static bool[] GetClusterInliers(List<List<float>> points, List<int> labels, float threshold)
        {
            List<Clustering.VectorCluster<int>> clusters = Clustering.Clusterizer.CreateClusters<int>(points, labels, threshold);

            var largestCountQuery = clusters.Max((item) => item.Members);
            Clustering.VectorCluster<int> largestCluster = null;
            foreach (Clustering.VectorCluster<int> c in clusters)
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

        public static List<VectorCluster<T>> CreateClusters<T>(List<List<float>> points, List<T> labels, float maxDistance)
        {
            List<VectorCluster<T>> initialClusters = new List<VectorCluster<T>>();
            for (int i = 0; i < points.Count; i++)
            {
                initialClusters.Add(new VectorCluster<T>(points[i], labels[i]));
            }

            return Clusterize<T>(initialClusters, maxDistance);
        }

        public static List<VectorCluster<T>> Clusterize<T>(List<VectorCluster<T>> clusters, float maxDistance)
        {
            float bestDistance = float.PositiveInfinity;
            Tuple<VectorCluster<T>, VectorCluster<T>> bestPair = null;

            for (int i = 0; i < clusters.Count; i++)
            {
                VectorCluster<T> c1 = clusters[i];
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    VectorCluster<T> c2 = clusters[j];
                    float dist = c1.DistanceFrom(c2);
                    if (dist < bestDistance)
                    {
                        bestPair = new Tuple<VectorCluster<T>, VectorCluster<T>>(c1, c2);
                        bestDistance = dist;
                    }
                }
            }
            if (bestDistance < maxDistance)
            {
                VectorCluster<T> c1 = bestPair.Item1;
                VectorCluster<T> c2 = bestPair.Item2;
                clusters.Remove(c1);
                clusters.Remove(c2);
                clusters.Add(new VectorCluster<T>(c1, c2));
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
