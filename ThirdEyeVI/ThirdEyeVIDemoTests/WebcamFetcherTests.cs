using Microsoft.VisualStudio.TestTools.UnitTesting;
using WristbandCsharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp.Tests
{
    [TestClass()]
    public class WebcamFetcherTests
    {
        static WebcamFetcher Cam = new WebcamFetcher(640, 640);
        [TestMethod()]
        public void CallWebcamFetched()
        {
            Cam.FrameFetched += i_FrameFetched;
            Cam.Start();
        }
        static void i_FrameFetched(object sender, FrameFetchedEventArgs e)
        {
            Console.WriteLine("Image was Fetched");
            Cam.Stop();
            //Environment.Exit(0);
        }
    }
}
