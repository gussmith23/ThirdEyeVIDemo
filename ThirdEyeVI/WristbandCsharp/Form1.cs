using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV.UI;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO.Ports;
using System.IO;
using Speech;
using System.Threading;
using ObjectSpeechRecognizer;
using CAPIStreamServer;
using System.Runtime.InteropServices;
using CAPIStreamClient;

namespace WristbandCsharp
{
    public partial class Form1 : Form
    {

        [DllImport(@"ffmpeg_export.dll")]
        private static extern IntPtr convertYUVtoRGB(IntPtr yuv_data, int width, int height);

        [DllImport(@"ffmpeg_export.dll")]
        private static extern void initFrameConverter(int width, int height);

        [DllImport(@"ffmpeg_export.dll")]
        private static extern void deinitFrameConverter();

        int camera = 0;
        
        Capture cap;
        Image<Bgr,Byte> image;
        CMTTracker cmtTracker = null;
        Arduino arduino;
        Boolean tracking = false;
        SpeechEngine speechEngine = null;
        Thread speechThread;
        AsyncSpeechWorker speechWorker;
        private const string triggerPhrase = "locate";
        private List<string> itemsAvailableForLocation;

        Tracker tracker = null;

        Thread objectRecognizerThread;
        AsyncObjectRecognizerWorker objectRecognizerWorker;

        SURFEngine testEngine = null;

        // Streaming.
        // Expected width and height.
        int stream_width = 640, stream_height = 480;
        
        // Getting commands from cart
        //bool connect_to_cart = false;
        ConnectionControllerClient cart_connection;
        string cart_addr = "";
        int cart_port = -1;

        // Show from cam handler.
        EventHandler ShowFromCamHandler;   

        /**
         * These functons are used to listen for incoming cart connections.
         */
        public void listenForPacket(){
            (new Thread(listenThreadLogic)).Start();
        }
        public void listenThreadLogic()
        {
            while (true)
            {
                CAPIStreamCommon.SocketData s = cart_connection.ReceiveDataPacket(cart_connection.stream);
                if (s != null && s.message_type == CAPIStreamCommon.PacketType.WORK_ACK)
                {
                    getItemToTrackViaNetwork(s);
                }
            }
        }

        public Form1()
        {

            InitializeComponent();

            #region setup server to wait for glove connection
            ServerController server = new ServerController();
            //for every delegate you want to functino
            server.registerDelegate(CAPIStreamCommon.PacketType.VIDEO_FRAME, new ImageWork(doWorkOnData));
            server.startServer(CAPIStreamServer.ConnectionType.TCP);
            #endregion

            #region setup decoder
            initFrameConverter(stream_width, stream_height);
            #endregion

            #region combo box 1
            itemsAvailableForLocation = new List<string>();
            string[] itemNames = Directory.GetFiles("itemsToTrack/", "*.jpg");
            foreach (string s in itemNames)
            {
                string name = System.Text.RegularExpressions.Regex.Replace(s, "itemsToTrack/", "");
                name = System.Text.RegularExpressions.Regex.Replace(name, ".jpg", "");
                comboBox1.Items.Add(name);
                itemsAvailableForLocation.Add(name);
            }
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.SelectedIndex = 0;
            #endregion

            #region combo box 2
            RefreshSerialPortList();
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            //comboBox2.SelectedIndex = 0;
            #endregion

            // Haptic feedback starts disabled
            checkBox1.Enabled = false;


            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            cap = new Capture(camera);
            
            
            // 896x504
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 504.0);
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 896.0);
            // 928x522
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 522.0);
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 928.0);
            //// 1024x576
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 576.0);
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 1024.0);
            // 1664x936
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 936.0);
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 1664.0);
            // 1152x648
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 648.0);
            //cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 1152.0);
            // 1920x1080
            cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 1080.0);
            cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 1920.0);
            
            
            ShowFromCamHandler = new EventHandler(ShowFromCam);

        }

        private byte[] doWorkOnData(CAPIStreamCommon.SocketData d)
        {
            Image<Bgr, Byte> return_image;

            #region Convert to usable image + place in return_image.

            if (d == null) return null;

            int size = stream_width * stream_height * 3;
            byte[] rgb_data = new byte[size];
            unsafe
            {
                IntPtr byteArray = Marshal.AllocHGlobal(d.data.Length);
                Marshal.Copy(d.data, 0, byteArray, d.data.Length);
                IntPtr rgb_data_ptr;
                rgb_data_ptr = convertYUVtoRGB(byteArray, stream_width, stream_height);
                Marshal.FreeHGlobal(byteArray);
                Marshal.Copy(rgb_data_ptr, rgb_data, 0, size);
            }
            Image<Bgr, Byte> converted_image = new Image<Bgr, Byte>(stream_width, stream_height);
            Buffer.BlockCopy(rgb_data, 0, converted_image.Data, 0, size);
            //CvInvoke.cvShowImage("frame", image);
            //CvInvoke.cvWaitKey(1);

            return_image = converted_image;
        
            #endregion            

            if (cmtTracker != null)
            {
                return_image = cmtTracker.Process(return_image);

                #region audio feedback
                if (checkBox2.Checked)
                {
                    // Get direction to force in
                    int direction = CMTTracker.findDirection(cmtTracker.centerOfObject, new PointF(stream_width / 2, stream_height / 2));

                    switch (direction)
                    {
                        case 0:
                            speechWorker.setDirection("left");
                            break;
                        case 1:
                            speechWorker.setDirection("down");
                            break;
                        case 2:
                            speechWorker.setDirection("right");
                            break;
                        case 3:
                            speechWorker.setDirection("up");
                            break;
                        case -1:
                            speechWorker.setDirection("");
                            break;
                        case 5:
                            speechWorker.setDirection("forward");
                            break;
                    }
                }
                #endregion

            }

            pictureBox1.Image = return_image.ToBitmap();

            return null;
        }

        private void getItemToTrackViaNetwork(CAPIStreamCommon.SocketData d)
        {
            string item = System.Text.Encoding.ASCII.GetString(d.data);

            cmtTracker = new CMTTracker("itemsToTrack/" + item + ".jpg");
        }
        

        /// <summary>
        /// Initializes tracker with a "Tracker" type object, so that Tracker.Process may be called on the next iteration of ShowFromCam.
        /// </summary>
        /// <param name="trackType">The tracking type. So far, the types supported are CMT + SURF (0) and Pure SURF (1).</param>
        /// <param name="itemImage">The image of the item to track.</param>
        private void initializeTrackerWithSettings(int trackType, Image<Bgr, Byte> itemImage)
        {
            switch (trackType)
            {
                // CMT + SURF
                case 0:
                    tracker = new CMTTracker(itemImage);
                    break;
                case 1:
                    throw new NotImplementedException("Write the code for Pure SURF tracking!");
                    break;
                default:
                    tracker = null;
                    break;
            }
        }

        private void initializeTrackerWithSettings(int trackType, string fileName)
        {
            initializeTrackerWithSettings(trackType, new Image<Bgr, Byte>(fileName));
        }

        private void imageBox1_Click(object sender, EventArgs e)
        {

        }

        public void setImage(Image<Bgr, byte> img)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// "Start" button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            // Return if nothing selected.
            if ((string)comboBox1.SelectedItem == "") return;

            // For right now - 1 if pure SURF checked, 0 otherwise. Thus, we'll default to SURF and CMT.
            int trackType = Convert.ToInt32(radioButton5.Checked);

            // Call the override, passing in the file name.
            initializeTrackerWithSettings(
                trackType, 
                string.Format("itemsToTrack/{0}.jpg", comboBox1.SelectedItem)
                );
        }

        void ShowFromCam(object sender, EventArgs e)
        {

            // Get image.
            image = null;
            while (image == null) image = cap.QueryFrame();
            Image<Bgr, Byte> returnimage = image;

            int width = (int)cap.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH);
            int height = (int)cap.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT);


            // If a tracker (of any kind) is initialized:
            if (tracker != null)
            {
                // Call Process on our tracker; will either process with CMT or SURF
                returnimage = tracker.Process(image);

            }

            if (cmtTracker != null)
            {
                try
                {
                    returnimage = cmtTracker.Process(image);
                }
                catch (Exception exception)
                {

                }

                // Send to Arduino
                if (checkBox1.Checked)
                {
                    try
                    {
                        

                        //arduino.Process(tracker.roi, tracker.centerOfObject, new PointF(pictureBox1.Width / 2, pictureBox1.Height / 2));
                        arduino.SendPacket(CMTTracker.findDirection(cmtTracker.centerOfObject, new PointF(width / 2, height / 2)),
                            100, 100);
                    }
                    // COM Port died.
                    catch (System.IO.IOException ioException)
                    {
                        Console.WriteLine(ioException.Message);
                        DestroySerial();
                    }
                }
            

            
                // Speech
                if (checkBox2.Checked)
                {
                    // Get direction to force in
                    int direction = CMTTracker.findDirection(cmtTracker.centerOfObject, new PointF(width / 2, height / 2));

                    switch (direction)
                    {
                        case 0:
                            speechWorker.setDirection("left");
                            break;
                        case 1:
                            speechWorker.setDirection("down");
                            break;
                        case 2:
                            speechWorker.setDirection("right");
                            break;
                        case 3:
                            speechWorker.setDirection("up");
                            break;
                        case -1:
                            speechWorker.setDirection("");
                            break;
                        case 5:
                            speechWorker.setDirection("forward");
                            break;
                    }
                }

                if (checkBox3.Checked)
                {
                    // Get direction to force in
                    int direction = CMTTracker.findDirection(cmtTracker.centerOfObject, new PointF(pictureBox1.Width / 2, pictureBox1.Height / 2));

                    switch (direction)
                    {
                        case 0:
                            label5.Text = "right";
                            break;
                        case 1:
                            label5.Text = "up";
                            break;
                        case 2:
                            label5.Text = "left";
                            break;
                        case 3:
                            label5.Text = "down";
                            break;
                        case -1:
                            label5.Text = "";
                            break;
                    }

                }

            }
            pictureBox1.Image = returnimage.ToBitmap();

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            // If the change happened via the voice recognizer...
            if (radioButton2.Checked)
            {
                cmtTracker = new CMTTracker(
                string.Format("itemsToTrack/{0}.jpg", comboBox1.SelectedItem)
                );
            }


        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // This statement is not really necessary, but i'm leaving it for now.
            cmtTracker = null;

            tracker = null;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            arduino = new Arduino((string)comboBox2.SelectedItem);
            checkBox1.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DestroySerial();
        }

        private void RefreshSerialPortList()
        {
            // Combo box 2 - Arduino selection
            comboBox2.Items.Clear();
            comboBox2.Items.AddRange(SerialPort.GetPortNames());
        }

        private void button6_Click(object sender, EventArgs e)
        {
            RefreshSerialPortList();
        }

        private void DestroySerial()
        {
            arduino = null;
            checkBox1.Checked = false;
            checkBox1.Enabled = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                speechWorker = new AsyncSpeechWorker();
                speechThread = new Thread(speechWorker.doWork);
                speechThread.Start();
                // Wait
                while(!speechThread.IsAlive);
            }
            else
            {
                speechWorker.requestStop();
                speechThread.Join();
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            label5.Visible = checkBox3.Checked;
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged_1(object sender, EventArgs e)
        {
            
        }

        private void radioButton3_CheckedChanged_1(object sender, EventArgs e)
        {
            comboBox1.Enabled = radioButton3.Checked;
            button1.Enabled = radioButton3.Checked;
        }

        private void radioButton2_CheckedChanged_1(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {

                // New worker
                objectRecognizerWorker = new AsyncObjectRecognizerWorker(
                    new ObjectRecognizer(triggerPhrase, itemsAvailableForLocation),
                    delegate(string item)
                    {
                        // was the item in the list?
                        bool found = false;


                        // Find item in the list, set list to that item.
                        System.Collections.IEnumerator ie = comboBox1.Items.GetEnumerator();
                        for (int i = 0; ie.MoveNext()&&!found; i++)
                        {
                            if ((string)ie.Current == item)
                            {
                                found = true;

                                if (InvokeRequired)
                                {
                                    this.Invoke((Action)(() => comboBox1.SelectedIndex = i));

                                    // Trigger event (in the case that our index didn't actually change)
                                    if (comboBox1.InvokeRequired)
                                        comboBox1.Invoke(new EventHandler(comboBox1_SelectedIndexChanged), null, new EventArgs());
                                    else 
                                        comboBox1_SelectedIndexChanged(null, new EventArgs());
                                }
                                else comboBox1.SelectedIndex = i;

                            }
                            else ; // NOT FOUND
                        }

                        if (found)
                        {
                            // DO WE WANNA STOP LISTENING WHEN FOUND?
                            /*
                            // Request that worker stops (should have been requested by this point, but to be thorough)
                            objectRecognizerWorker.requestStop();

                            // End the listener thread.
                            objectRecognizerThread.Join();
                            */


                            
                        }
                    }
                    );

                // New thread
                objectRecognizerThread = new Thread(objectRecognizerWorker.doWork);

                // Start thread
                objectRecognizerThread.Start();

                // Wait
                while(!objectRecognizerThread.IsAlive);

            }

            else
            {
                // Else, we shut down the thread and the worker.
                objectRecognizerWorker.requestStop();
                objectRecognizerThread.Abort();
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            cmtTracker = new CMTTracker(
                "itemsToTrack/Frosted Flakes.jpg"
                );
        }

        private void button7_Click(object sender, EventArgs e)
        {
            cmtTracker = new CMTTracker(
                "itemsToTrack/Bran Flakes.jpg"
                );
        }

        private void button8_Click(object sender, EventArgs e)
        {
            cmtTracker = new CMTTracker(
                "itemsToTrack/Ketchup.jpg"
                );
        }

        private void button9_Click(object sender, EventArgs e)
        {
            cmtTracker = new CMTTracker(
                "itemsToTrack/Progresso.jpg"
                );
        }

        private void button10_Click(object sender, EventArgs e)
        {
            cmtTracker = new CMTTracker(
                "itemsToTrack/Sriracha.jpg"
            );
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e){
            // The function for radioButton4 should cover all cases at the moment.
            //tracker.trackWithCMT = false;
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (cmtTracker != null) cmtTracker.trackWithCMT = radioButton4.Checked;
        }

        private void radioButton5_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                Application.Idle += ShowFromCamHandler;
            }
            else
            {
                Application.Idle -= ShowFromCamHandler;
            }
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        /**
         * Connect to cart
         */
        private void button8_Click_1(object sender, EventArgs e)
        {
            #region setup cart connection

            cart_addr = textBox1.Text;
            cart_port = Int32.Parse(textBox2.Text);
            
            cart_connection = new ConnectionControllerClient();
            cart_connection.configureConnection(CAPIStreamClient.ConnectionType.TCP, cart_addr, cart_port);

            listenForPacket();
            
            #endregion
        }

    }

}
