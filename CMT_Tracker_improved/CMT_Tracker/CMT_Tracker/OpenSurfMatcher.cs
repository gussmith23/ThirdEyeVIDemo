using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using System.Drawing;

namespace CMT_Tracker
{
    class OpenSurfMatcher<labelType> where labelType : new()
    {
        private Matrix<float> keypoints;
        private Matrix<float> features;

        public OpenSurfMatcher()
        {
            this.keypoints = null;
            this.features = null;
        }

        public void AddFeatures(Matrix<float> feat)
        {
            this.features = feat;
        }


        /*public void getMatches(Matrix<float> ipts2_feat, List<PointF> ipts2_kp, Matrix<int> matches, Matrix<float> matches_dist)
        {
          float dist, d1=float.PositiveInfinity, d2=float.PositiveInfinity;
          int f1=0, f2=0;
          
          for (int i = 0; i < ipts2_feat.Rows; i++) 
          {
            d1 = float.PositiveInfinity;
            d2 = float.PositiveInfinity;

            for (int j = 0; j < this.features.Rows; j++) 
            {
              //get distance
                float sum=0.0f;
                for(int k=0; k<64; k++) sum += (this.features[i,k] - ipts2_feat[j,k])*(this.features[i,k] - ipts2_feat[j,k]);
                dist = (float)Math.Sqrt(sum);

              if(dist<d1) // if this feature matches better than current best
              {
                d2 = d1;
                d1 = dist;
                f1=i; f2=j;
              }
              else if(dist<d2) // this feature matches better than second best
              {
                d2 = dist;
              }
            }

            // If match has a d1:d2 ratio < 0.65 ipoints are a match
            if(d1/d2 < 0.65) 
            { 
              // Store the change in position
              ipts1[i].dx = match->x - ipts1[i].x; 
             ipts1[i].dy = match->y - ipts1[i].y;
             matches.push_back(std::make_pair(ipts1[i], *match));
              //matches_dist
            }
          }
        }

        public void getMatches(Matrix<float> ipts2_feat, MKeyPoint[] ipts2_kp, Matrix<int> matches, Matrix<float> matches_dist)
        {
          float dist, d1=float.PositiveInfinity, d2=float.PositiveInfinity;
           int f1=0, f2=0;

           for (int i = 0; i < ipts2_feat.Rows; i++) 
          {
            d1 = float.PositiveInfinity;
            d2 = float.PositiveInfinity;

            for (int j = 0; j < this.features.Rows; j++) 
            {
              //get distance
                float sum=0.0f;
                for(int k=0; k<64; k++)
                  sum += (this.features[i,k] - ipts2_feat[j,k])*(this.features[i,k] - ipts2_feat[j,k]);
                dist = (float)Math.Sqrt(sum);



              if(dist<d1) // if this feature matches better than current best
              {
                d2 = d1;
                d1 = dist;
                 f1=i; f2=j;
              }
              else if(dist<d2) // this feature matches better than second best
              {
                d2 = dist;
              }
            }

            // If match has a d1:d2 ratio < 0.65 ipoints are a match
            if(d1/d2 < 0.65) 
            { 
              // Store the change in position
              matches[i,0]=f1;
              matches[i,1]=f2;
              //matches_dist
            }
          }
        }*/
    }
}
