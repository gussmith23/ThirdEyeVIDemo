using CAPIStreamServer;
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
using CAPIStreamClient;

namespace WristbandCsharp
{
    class NetworkFetcher : FrameFetcher //implementing interface
    {
        ServerController server; //This creates a server object

        NetworkFetcher()//we don't want it to start until we tell it to start,
                        //therefore, start() is outside the constructor
        {
            server = new ServerController();//Here the server object is initialized
        }

        public int Start()
        {            
            server.registerDelegate(CAPIStreamCommon.PacketType.VIDEO_FRAME, new ImageWork(doWorkOnData));
            return 0;
        }

        private byte[] doWorkOnData(SocketData s/*frame is passed*/)
        {

            throw new NotImplementedException();
        }

        public int Pause()
        {
            throw new NotImplementedException();
        }

        

        public int Stop()
        {
            throw new NotImplementedException();
        }
    }
}
