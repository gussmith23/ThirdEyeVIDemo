using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{

    

    class AsyncSpeechWorker
    {
        private volatile bool shouldStop = false;
        private volatile string direction = "";
        private Speech.SpeechEngine se;

        public AsyncSpeechWorker()
        {
            se = new Speech.SpeechEngine();
        }

        public void doWork()
        {
            while (!shouldStop)
            {
                if (direction != "") se.Speak(direction);
            }
        }

        public void requestStop()
        {
            shouldStop = true;
        }
        public void setDirection(string direction) {
            this.direction = direction;
        }

    }

   
}
