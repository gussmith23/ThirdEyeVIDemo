﻿using CAPIStreamServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAPIStreamCommon;
using Emgu.CV;
using Emgu.CV.Structure;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV.UI;
using Emgu.Util;
using System.IO.Ports;
using System.IO;
using Speech;
using System.Threading;
using ObjectSpeechRecognizer;
using System.Runtime.InteropServices;
namespace WristbandCsharp
{
    class CMT_Tracker_Processor : IFrameProcessor
    {
        private Image<Bgr, Byte> frame_fetched;        

        public void FrameFetched(object sender, FrameFetchedEventArgs e)
        {
            frame_fetched = e.Frame;          

        }
    }
}