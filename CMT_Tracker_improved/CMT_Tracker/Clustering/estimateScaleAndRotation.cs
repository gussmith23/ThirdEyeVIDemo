private void EstimateScaleAndRotation(PointF[] tracked_keypoints, int[] tracked_classes, ref float sc_est, ref float rot_est)
        {
            int numKP = tracked_keypoints.Length;
            List<int> sortedTrackOrder = IndexSort<int>(tracked_classes);
            //DumpList(ReorderList(tracked_keypoints, sortedTrackOrder));
            //DumpList(ReorderList(tracked_classes, sortedTrackOrder));

            
            List<float> scaleChange = new List<float>();
            List<float> angleChange = new List<float>();

            List<float>[] scaleChangeThread = new List<float>[numKP];
            List<float>[] angleChangeThread = new List<float>[numKP];

            Parallel.For (0 , numKP,
                i => {

                    scaleChangeThread[i] = new List<float>();
                    angleChangeThread[i] = new List<float>();
                PointF p1 = tracked_keypoints[sortedTrackOrder[i]];
                int c1 = tracked_classes[sortedTrackOrder[i]];
                for(int j=0; j<numKP; j++) {

                    if (i != j)
                    {
                        PointF p2 = tracked_keypoints[sortedTrackOrder[j]];
                        int c2 = tracked_classes[sortedTrackOrder[j]];
                        if (c2 != c1)
                        {
                            // Compute distance
                            float dist = L2Norm(p1, p2);
                            float deltaS = dist / mSelectedKeypointDistances[c1 - 1, c2 - 1];
                            scaleChangeThread[i].Add(deltaS);

                            // Compute angle
                            float angle = PointAngle(p1, p2);
                            float deltaA = BoundAngle(angle - mSelectedKeypointAngles[c1 - 1, c2 - 1]);
                            angleChangeThread[i].Add(deltaA);
                        }
                    }
                }          
            });

            for (int i = 0; i < scaleChangeThread.Length; i++)
            {
                scaleChange.AddRange(scaleChangeThread[i]);
                angleChange.AddRange(angleChangeThread[i]);
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