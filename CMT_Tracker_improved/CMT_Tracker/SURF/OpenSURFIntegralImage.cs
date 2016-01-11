using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace OpenSURF
{
    public class OpenSURFIntegralImage
    {
        const float cR = .2989f;
        const float cG = .5870f;
        const float cB = .1140f;

        internal float[,] Matrix;
        public int Width, Height;
        private int MatrixNumRows;
        private int MatrixNumCols;

        public float this[int y, int x]
        {
            get { return Matrix[y, x]; }
            set { Matrix[y, x] = value; }
        }

        private OpenSURFIntegralImage(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            this.Matrix = new float[height, width];
            MatrixNumRows = Matrix.GetLength(0);
            MatrixNumCols = Matrix.GetLength(1);
        }

        public float[,] GetIIMatrix()
        {
            return Matrix;
        }
        /// <summary>
        /// This method uses EmguCV 
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static OpenSURFIntegralImage FromImage(Bitmap image)
        {
            OpenSURFIntegralImage pic = new OpenSURFIntegralImage(image.Width, image.Height);

            // Check if bitmap already in grayscale
            bool GrayScale = (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            if (!GrayScale)
                image = new Image<Bgra, Byte>(image).Convert<Gray, Byte>().ToBitmap();

            float rowsum = 0;
            for (int x = 0; x < image.Width; x++)
            {
                Color c = image.GetPixel(x, 0);

                rowsum += c.R;

                pic[0, x] = rowsum;
            }


            for (int y = 1; y < image.Height; y++)
            {
                rowsum = 0;
                for (int x = 0; x < image.Width; x++)
                {
                    Color c = image.GetPixel(x, y);

                    rowsum += c.R;

                    // integral image is rowsum + value above        
                    pic[y, x] = rowsum + pic[y - 1, x];
                }
            }

            return pic;
        }

        /*

        public static IntegralImage FromImage(Bitmap image)
        {
            IntegralImage pic = new IntegralImage(image.Width, image.Height);

            // Check if bitmap already in grayscale
            bool GrayScale = (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            float rowsum = 0;
            for (int x = 0; x < image.Width; x++)
            {
                Color c = image.GetPixel(x, 0);

                if (!GrayScale)
                    rowsum += (float)Math.Round(cR * c.R + cG * c.G + cB * c.B);
                else
                    rowsum += c.R;            
            
                pic[0, x] = rowsum;
            }


            for (int y = 1; y < image.Height; y++)
            {
                rowsum = 0;
                for (int x = 0; x < image.Width; x++)
                {
                    Color c = image.GetPixel(x, y);

                    if (!GrayScale)
                        rowsum += (float)Math.Round(cR * c.R + cG * c.G + cB * c.B);
                    else
                        rowsum += c.R;
                
                    // integral image is rowsum + value above        
                    pic[y, x] = rowsum + pic[y - 1, x];
                }
            }

            return pic;
        }
    
        */


        unsafe public float BoxIntegral(int row, int col, int rows, int cols)
        {
            // The subtraction by one for row/col is because row/col is inclusive.
            int r1 = Math.Min(row, Height) - 1;
            int c1 = Math.Min(col, Width) - 1;
            int r2 = Math.Min(row + rows, Height) - 1;
            int c2 = Math.Min(col + cols, Width) - 1;

            float A = 0, B = 0, C = 0, D = 0;

            fixed (float* ptrMatrix = Matrix)
            {
                if (r1 >= 0 && c1 >= 0)
                    A = ptrMatrix[r1 * MatrixNumCols + c1];
                //A = Matrix[r1, c1];

                if (r1 >= 0 && c2 >= 0)
                    B = ptrMatrix[r1 * MatrixNumCols + c2];
                //B = Matrix[r1, c2];

                if (r2 >= 0 && c1 >= 0)
                    C = ptrMatrix[r2 * MatrixNumCols + c1];
                //C = Matrix[r2, c1];

                if (r2 >= 0 && c2 >= 0)
                    D = ptrMatrix[r2 * MatrixNumCols + c2];
                //D = Matrix[r2, c2];
            }
            return Math.Max(0, A - B - C + D);
        }

        unsafe public float BoxIntegral(float* ptrMatrix, int row, int col, int rows, int cols)
        {
            // The subtraction by one for row/col is because row/col is inclusive.
            int r1 = Math.Min(row, Height) - 1;
            int c1 = Math.Min(col, Width) - 1;
            int r2 = Math.Min(row + rows, Height) - 1;
            int c2 = Math.Min(col + cols, Width) - 1;

            float A = 0, B = 0, C = 0, D = 0;

            {
                if (r1 >= 0 && c1 >= 0)
                    A = ptrMatrix[r1 * MatrixNumCols + c1];
                //A = Matrix[r1, c1];

                if (r1 >= 0 && c2 >= 0)
                    B = ptrMatrix[r1 * MatrixNumCols + c2];
                //B = Matrix[r1, c2];

                if (r2 >= 0 && c1 >= 0)
                    C = ptrMatrix[r2 * MatrixNumCols + c1];
                //C = Matrix[r2, c1];

                if (r2 >= 0 && c2 >= 0)
                    D = ptrMatrix[r2 * MatrixNumCols + c2];
                //D = Matrix[r2, c2];
            }
            return Math.Max(0, A - B - C + D);
        }

        /// <summary>
        /// Get Haar Wavelet X repsonse
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public float HaarX(int row, int column, int size)
        {
            return BoxIntegral(row - size / 2, column, size, size / 2)
              - 1 * BoxIntegral(row - size / 2, column - size / 2, size, size / 2);
        }

        unsafe public float HaarX(float* ptrMatrix, int row, int column, int size)
        {
            return BoxIntegral(ptrMatrix, row - size / 2, column, size, size / 2)
              - 1 * BoxIntegral(ptrMatrix, row - size / 2, column - size / 2, size, size / 2);
        }

        /// <summary>
        /// Get Haar Wavelet Y repsonse
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public float HaarY(int row, int column, int size)
        {
            return BoxIntegral(row, column - size / 2, size / 2, size)
              - 1 * BoxIntegral(row - size / 2, column - size / 2, size / 2, size);
        }

        unsafe public float HaarY(float* ptrMatrix, int row, int column, int size)
        {
            return BoxIntegral(ptrMatrix, row, column - size / 2, size / 2, size)
              - 1 * BoxIntegral(ptrMatrix, row - size / 2, column - size / 2, size / 2, size);
        }
    }
}
