using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using OpenSURFcs;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;

namespace OpenSURFDemo
{
  public partial class DemoSURF : Form
  {

    public DemoSURF()
    {
      InitializeComponent();
    }

    List<IPoint> ipts = new List<IPoint>();

    private void btnRunSurf_Click(object sender, EventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.ShowDialog();
      string pathToFile = openFileDialog.FileName;

      Stopwatch watch = new Stopwatch();
      watch.Start();

      try
      {
        // Load an Image
        Bitmap img = new Bitmap(pathToFile);
        pbMainPicture.Image = img;

        Stopwatch sw = new Stopwatch();

        sw.Reset();
        sw.Start();
        Bitmap img_gray = new Image<Bgra, Byte>(img).Convert<Gray, Byte>().ToBitmap();
        sw.Stop();
        Console.WriteLine("Color Conversion execution time = {0}ms", sw.ElapsedMilliseconds);

        sw.Reset();
        sw.Start();
        // Create Integral Image
        IntegralImage iimg = IntegralImage.FromImage(img_gray);
        sw.Stop();
        Console.WriteLine("integral image execution time = {0}ms", sw.ElapsedMilliseconds);

        sw.Reset();
        sw.Start();
        // Extract the interest points
        //ipts = FastHessian.getIpoints(0.0005f, 3, 1, iimg);
        //ipts = FastHessian.getIpoints(0.003f, 3, 1, iimg);
        ipts = FastHessian.getIpoints(25f, 3, 1, iimg);
        sw.Stop();
        Console.WriteLine("ip detection execution time = {0}ms", sw.ElapsedMilliseconds);

        sw.Reset();
        sw.Start();
        // Describe the interest points
        SurfDescriptor.DecribeInterestPoints(ipts, false, false, iimg);
        sw.Stop();
        Console.WriteLine("ip description execution time = {0}ms", sw.ElapsedMilliseconds);

        // Draw points on the image
        PaintSURF(img, ipts);

        bool DisplayIP = false;
        if (DisplayIP)
        {
            Console.WriteLine("y,x,scale,laplacian");

            for (int i = 0; i < ipts.Count; i++)
            {
                Console.WriteLine("{0},{1},{2},{3}", ipts[i].y, ipts[i].x, ipts[i].scale, ipts[i].laplacian);
            }
        }
          
      }
      catch (Exception ex)
      {

      }

      watch.Stop();
      this.Text = "DemoSURF - Elapsed time: " + watch.Elapsed + 
                  " for " + ipts.Count + "points";
    }

    private void PaintSURF(Bitmap img, List<IPoint> ipts)
    {
      Graphics g = Graphics.FromImage(img);
      
      Pen redPen = new Pen(Color.Red);
      Pen bluePen = new Pen(Color.Blue);
      Pen myPen;

      foreach (IPoint ip in ipts)
      {
        int S = 2 * Convert.ToInt32(2.5f * ip.scale);
        int R = Convert.ToInt32(S / 2f);

        Point pt = new Point(Convert.ToInt32(ip.x), Convert.ToInt32(ip.y));
        Point ptR = new Point(Convert.ToInt32(R * Math.Cos(ip.orientation)),
                     Convert.ToInt32(R * Math.Sin(ip.orientation)));

        myPen = (ip.laplacian > 0 ? bluePen : redPen);

        g.DrawEllipse(myPen, pt.X - R, pt.Y - R, S, S);
        //g.DrawLine(new Pen(Color.FromArgb(0, 255, 0)), new Point(pt.X, pt.Y), new Point(pt.X + ptR.X, pt.Y + ptR.Y));
      }

      //pbMainPicture.Image.Save("result_image.jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
      pbMainPicture.Image.Save("result_image.png", System.Drawing.Imaging.ImageFormat.Png);
    }

  }  // DemoApp
} // OpenSURFDemo
