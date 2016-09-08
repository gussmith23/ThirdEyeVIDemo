using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;

namespace Speech
{
    class SpeechEngine
    {
        SpeechSynthesizer sSynth;
        PromptBuilder pBuilder;

        public SpeechEngine()
        {
            Initialize();            
        }

        private void Initialize()
        {
            sSynth = new SpeechSynthesizer();
            pBuilder = new PromptBuilder();
        }

        public void Speak(string speechString)
        {
            pBuilder.ClearContent();
            pBuilder.AppendText(speechString);
            sSynth.Speak(pBuilder);
        }

        public void SpeakAsync(string speechString)
        {
            pBuilder.ClearContent();
            pBuilder.AppendText(speechString);
            sSynth.SpeakAsync(pBuilder);
        }

    }
}
