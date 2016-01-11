using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;

namespace WristbandCsharp
{
    class WristbandCsharp
    {
        [STAThread]
        static void Main(string[] args)
        {
            Form1 form = new Form1();
            System.Windows.Forms.Application.Run(form);

        }
    }
}
