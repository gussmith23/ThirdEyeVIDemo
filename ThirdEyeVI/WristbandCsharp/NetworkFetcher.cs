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


namespace WristbandCsharp
{
    class NetworkFetcher : IFrameFetcher  //implementing interface
    {
        ServerController server; //This creates a server object

        public event EventHandler FrameFetched;//This 

        object objectLock = new object();

        event EventHandler IFrameFetcher.FrameFetched
        {
            add
            {
                lock (objectLock)
                {
                    FrameFetched += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    FrameFetched -= value;
                }
            }
        }


        NetworkFetcher()//we don't want it to start until we tell it to start,
                        //therefore, start() is outside the constructor
        {
            server = new ServerController();//Here the server object is initialized
        }

        public int Start()
        {
           //server.registerDelegate(CAPIStreamCommon.PacketType.VIDEO_FRAME, new ImageWork(FetchImageFromNetwork));
            return 0;
        }

        private byte[] FetchImageFromNetwork(SocketData s/*frame is passed*/)
        {
            FrameFetched?.Invoke(this, new EventArgs());
            
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
