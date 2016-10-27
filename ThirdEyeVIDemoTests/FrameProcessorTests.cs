using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThirdEyeVIDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


namespace ThirdEyeVIDemo.Tests
{
    [TestClass()]
    public class FrameProcessorTests
    {
        static string test = "test";
        static FrameProcessor FrmPrcs = new FrameProcessor(0, test);
        [TestMethod()]
        public void CallFrameProcessor()
        {
            FrmPrcs.FrameProcessed += i_FrameProcessed;
            FrmPrcs.Start();
        }
        static void i_FrameProcessed(object sender, FrameProcessedEventArgs e)
        {
            Console.WriteLine("Frame was Processed");
            Console.WriteLine("X = " + e.roi.X);
            Console.WriteLine("Y = " + e.roi.Y);
            FrmPrcs.Stop();
            //Environment.Exit(0);
        }
    }
}