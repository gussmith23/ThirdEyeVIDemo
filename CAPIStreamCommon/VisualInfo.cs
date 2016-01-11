using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;


namespace CAPIStreamCommon
{
    public class CAPIPoint
    {
        public UInt32 x { get; set; }
        public UInt32 y { get; set; }
        public CAPIPoint(UInt32 x, UInt32 y)
        {
            this.x = x;
            this.y = y;
        }
    }
    public class CAPISize
    {
        public UInt32 width { get; set; }
        public UInt32 height { get; set; }
        
        public CAPISize(UInt32 width, UInt32 height)
        {
            this.width = width;
            this.height = height;
        }
    }
    public class CAPIRoi
    {
        CAPIPoint origin;
        CAPISize size;
        public CAPIRoi(UInt32 x, UInt32 y, UInt32 width, UInt32 height)
        {
            origin = new CAPIPoint(x, y);
            size = new CAPISize(width, height);
        }
    }
    public class CAPIFrame
    {
        UInt32 roi_count;
        List<CAPIRoi> rois;
        public CAPIFrame(byte[] data)
        {
            roi_count = (UInt32)((((((data[3] << 8) | data[2]) << 8) | data[1]) << 8) | data[0]);
            byte[] tmpData = new byte[16];
            rois = new List<CAPIRoi>();
            Array.Copy(data, 12, tmpData, 0, 16);
            for (int i = 0; i < roi_count; ++i)
            {
                UInt32 x, y, width, height;
                x = (UInt32)((((((data[3] << 8) | data[2]) << 8) | data[1]) << 8) | data[0]);
                y = (UInt32)((((((data[7] << 8) | data[6]) << 8) | data[5]) << 8) | data[4]);
                width = (UInt32)((((((data[11] << 8) | data[10]) << 8) | data[9]) << 8) | data[8]);
                height = (UInt32)((((((data[15] << 8) | data[14]) << 8) | data[13]) << 8) | data[12]);
                rois.Add(new CAPIRoi(x, y, width, height));
                if (i != roi_count - 1) {
                    Array.Copy(data, 12 + (i + 1) * 16, tmpData, 0, 16);
                }
            }
        }
    }
    public class SURFKeypoint
    {
        public UInt32 laplacian { get; set; }
        public UInt32 r1 { get; set; }
        public UInt32 c1 { get; set; }
        public UInt32 r2 { get; set; }
        public UInt32 c2 { get; set; }
        public float scale { get; set; }
        public float orientation { get; set; }
        public float[] descriptor { get; set; }

        public SURFKeypoint()
        {
            descriptor = new float[64];
        }
        ~SURFKeypoint()
        {

        }

        static public SURFKeypoint[] extract_keypoints(byte[] data, int data_len)
        {
            int num = data_len / (28 + 64*4);
            SURFKeypoint[] points = new SURFKeypoint[num];
            byte[] tmpData = new byte[28 + 64*4];
            for (int i = 0; i < num; ++i)
            {
                Array.Copy(data, i * (28 + 64*4), tmpData, 0, 28 + 64*4);
                points[i] = new SURFKeypoint();

                //points[i].scale = (float)((((((tmpData[3] << 8) | tmpData[2]) << 8) | tmpData[1]) << 8) | tmpData[0]);
                points[i].scale = System.BitConverter.ToSingle(tmpData, 0);
                points[i].orientation = System.BitConverter.ToSingle(tmpData, 4);
                //points[i].orientation = (float)((((((tmpData[7] << 8) | tmpData[6]) << 8) | tmpData[5]) << 8) | tmpData[4]);
                points[i].laplacian = (UInt32)((((((tmpData[11] << 8) | tmpData[10]) << 8) | tmpData[9]) << 8) | tmpData[8]);
                points[i].r1 = (UInt32)((((((tmpData[15] << 8) | tmpData[14]) << 8) | tmpData[13]) << 8) | tmpData[12]);
                points[i].c1 = (UInt32)((((((tmpData[19] << 8) | tmpData[18]) << 8) | tmpData[17]) << 8) | tmpData[16]);
                points[i].r2 = (UInt32)((((((tmpData[23] << 8) | tmpData[22]) << 8) | tmpData[21]) << 8) | tmpData[20]);
                points[i].c2 = (UInt32)((((((tmpData[27] << 8) | tmpData[26]) << 8) | tmpData[25]) << 8) | tmpData[24]);

                for (int j = 0; j < 64; ++j)
                {
                    points[i].descriptor[j] = System.BitConverter.ToSingle(tmpData, 28 + j*4);
                    //Console.WriteLine("{0}", points[i].descriptor[j]);
                }
                //points[i].printKeypoint();
            }
            return points;
        }
        void printKeypoint()
        {
            Console.WriteLine("Scale: {0}, Orientation:{1}, R1:{2}, R2:{3}", scale, orientation, r1, r2);
        }

    }
    public class SURFKeypointMatch
    {
        public UInt32 model_id { get; set; }
        public UInt32 model_x { get; set; }
        public UInt32 model_y { get; set; }
        public float model_size { get; set; }
        public float model_angle { get; set; }
        public UInt32 frame_x { get; set; }
        public UInt32 frame_y { get; set; }
        public float frame_size { get; set; }
        public float frame_angle { get; set; }
        public float model_score { get; set; }
        
        public float[] descriptor { get; set; }

        public SURFKeypointMatch()
        {
            descriptor = new float[64];
        }

        static public SURFKeypointMatch[] extract_keypoint_matches(byte[] data, int data_len)
        {
            int num = data_len / (10*4);
            SURFKeypointMatch[] points = new SURFKeypointMatch[num];
            byte[] tmpData = new byte[10*4];
            for (int i = 0; i < num; ++i)
            {
                Array.Copy(data, i * (10*4), tmpData, 0, 10*4);
                points[i] = new SURFKeypointMatch();

                points[i].model_id = (UInt32)((((((tmpData[3] << 8) | tmpData[2]) << 8) | tmpData[1]) << 8) | tmpData[0]);
                points[i].model_x =  (UInt32)((((((tmpData[7] << 8) | tmpData[6]) << 8) | tmpData[5]) << 8) | tmpData[4]);
                points[i].model_y = (UInt32)((((((tmpData[11] << 8) | tmpData[10]) << 8) | tmpData[9]) << 8) | tmpData[8]);
                points[i].model_size = System.BitConverter.ToSingle(tmpData, 12);
                points[i].model_angle = System.BitConverter.ToSingle(tmpData, 16);
                points[i].frame_x = (UInt32)((((((tmpData[23] << 8) | tmpData[22]) << 8) | tmpData[21]) << 8) | tmpData[20]);
                points[i].frame_y = (UInt32)((((((tmpData[27] << 8) | tmpData[26]) << 8) | tmpData[25]) << 8) | tmpData[24]);
                points[i].frame_size = System.BitConverter.ToSingle(tmpData, 28);
                points[i].frame_angle = System.BitConverter.ToSingle(tmpData, 32);
                points[i].model_score = System.BitConverter.ToSingle(tmpData, 36);
                /*
                for (int j = 0; j < 64; ++j)
                {
                    points[i].descriptor[j] = System.BitConverter.ToSingle(tmpData, 28 + j*4);
                    //Console.WriteLine("{0}", points[i].descriptor[j]);
                }
                points[i].printKeypoint();
                */
            }
            return points;
        }
        void printKeypoint()
        {
            Console.WriteLine("Match X: {0}, Y:{1}, Frame X:{2}, Y:{3}", model_x, model_y, frame_x, frame_x);
        }

    }

}
