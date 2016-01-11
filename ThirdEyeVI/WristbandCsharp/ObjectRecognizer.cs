using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace ObjectSpeechRecognizer
{
    // Delcaration (in Form, etc)
    // private ObjectSpeechRecognizer.ObjectRecognizer objRec;
    // private string[] detectableObjects = new string[] { "Coke", "Pepsi", "Doritos", "Corn Flakes", "Frosted Flakes" };


    // Initialization (in Form_Load or Form constructor)
    //objRec = new ObjectSpeechRecognizer.ObjectRecognizer(detectableObjects);
    //objRec.SpeechRecognized += objRec_SpeechRecognized;


    // Event Handler declaration
    //void objRec_SpeechRecognized(string Text, float Confidence)
    //{
    //    if (label1.InvokeRequired)
    //    {
    //        label1.Invoke(new ObjectSpeechRecognizer.ObjectRecognizer.SpeechRecognizedEvent(objRec_SpeechRecognized), Text, Confidence);
    //    }
    //    else
    //    {
    //        label1.Text = Text;
    //    }
    //}


    // Recognition Invocation
    // objRec.Recognize(3000);      // 3 sec timeout
    // or
    // objRec.Recognize();          // No timeout


    public class ObjectRecognizer
    {
        private SpeechRecognitionEngine mRecog;

        public delegate void SpeechRecognizedEvent(string Text, float Confidence);
        public event SpeechRecognizedEvent SpeechRecognized;

        private string triggerPhrase = "Find";
        
        public ObjectRecognizer(string TriggerPhrase, List<string> objects)
        {
            triggerPhrase = TriggerPhrase;
            Init(objects.ToArray());
        }
        public ObjectRecognizer(string TriggerPhrase, string[] objects)
        {
            triggerPhrase = TriggerPhrase;
            Init(objects);
        }
        public void Recognize()
        {
            mRecog.Recognize();
        }
        public void Recognize(int timeout)
        {
            mRecog.Recognize(new TimeSpan(0, 0, 0, 0, timeout));
        }
        private void Init(string[] objects)
        {
            try
            {
                mRecog = new SpeechRecognitionEngine();

                GrammarBuilder builder = new GrammarBuilder();
                builder.Append(triggerPhrase);
                Grammar defaultGrammar = new Grammar(builder);
                defaultGrammar.Name = "Trigger Only";
                mRecog.LoadGrammar(defaultGrammar);

                builder = new GrammarBuilder();
                builder.Append(triggerPhrase);
                builder.Append(new Choices(objects));
                Grammar grammar = new Grammar(builder);
                grammar.Name = "Tracker Triggers";
                mRecog.LoadGrammar(grammar);

                // Configure audio input and events
                mRecog.SetInputToDefaultAudioDevice();
                mRecog.SpeechDetected += mRecog_SpeechDetected;
                mRecog.SpeechHypothesized += mRecog_SpeechHypothesized;
                mRecog.SpeechRecognized += mRecog_SpeechRecognized;
                mRecog.SpeechRecognitionRejected += mRecog_SpeechRecognitionRejected;
                mRecog.RecognizeCompleted += mRecog_RecognizeCompleted;
            }
            catch (Exception ex)
            {
            }
        }

        void mRecog_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            
        }

        void mRecog_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Console.WriteLine("Rejected: {0} = {1}", e.Result.Text, e.Result.Confidence);
            if (SpeechRecognized != null)
            {
                string Text = e.Result.Text.Trim();
                if (Text.StartsWith(triggerPhrase))
                {
                    Text = Text.Substring(triggerPhrase.Length);
                }
                Text = Text.Trim();
                SpeechRecognized(Text, e.Result.Confidence);
            }
        }

        void mRecog_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("Recognized: {0} = {1}", e.Result.Text, e.Result.Confidence);
            if (SpeechRecognized != null)
            {
                string Text = e.Result.Text.Trim();
                if (String.IsNullOrEmpty(Text)) return;
                if (Text.StartsWith(triggerPhrase))
                {
                    Text = Text.Substring(triggerPhrase.Length);
                }
                Text = Text.Trim();
                if (String.IsNullOrEmpty(Text)) return;
                SpeechRecognized(Text, e.Result.Confidence);

            }
        }
        void mRecog_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Console.WriteLine("Hypothesized: {0} = {1}", e.Result.Text, e.Result.Confidence);
        }

        void mRecog_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
        }
    }
}
