using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ROIStreaming
{
    public class ROIReceiver
    {
        private ClientServer.Server mServer;
        public delegate void NewROIsReceivedDelegate(List<Tuple<string, Image<Bgr, Byte>>> newROIs);
        public event NewROIsReceivedDelegate NewROIsReceived;

        public ROIReceiver(int localPort)
        {
            mServer = new ClientServer.Server();
            mServer.MaxClients = 10;
            mServer.ClientConnected += mServer_ClientConnected;
            mServer.ClientDisconnected += mServer_ClientDisconnected;
            mServer.ClientPacketReceived += mServer_ClientPacketReceived;
            mServer.Start(localPort);

            recState = ReceiverState.WaitingForBatchStart;
            rois = new List<Tuple<string, Image<Bgr, byte>>>();
        }
        public void Stop()
        {
            mServer.Shutdown();            
        }
        void mServer_ClientDisconnected(int ConnectionID)
        {
            Console.WriteLine("Client {0} disconnected.", ConnectionID);
        }

        void mServer_ClientConnected(int ConnectionID)
        {
            Console.WriteLine("Client {0} connected.", ConnectionID);
        }

        private enum ReceiverState : int
        {
            WaitingForBatchStart,
            WaitingForStringHeader,
            WaitingForString,
            WaitingForImageHeader,
            WaitingForImage,
            Invalid = 0xff
        }
        private ReceiverState recState;
        private int batchCount;
        private int inProgressLabelLength;
        private string inProgressLabel;
        private int inProgressExpectedSize;
        private Image<Bgr, Byte> inProgressImage;
        private List<Tuple<string, Image<Bgr, Byte>>> rois;


        private void mServer_ClientPacketReceived(int ConnectionID, byte[] data)
        {
            try
            {
                lock (rois)
                {
                    switch (recState)
                    {
                        case ReceiverState.WaitingForBatchStart:
                            // Receive a single integer, indicating the number of ROIs in the batch
                            if (data.Length == sizeof(int))
                            {
                                batchCount = BitConverter.ToInt32(data, 0);
                                recState = ReceiverState.WaitingForStringHeader;
                                Console.WriteLine("Receiver: Expecting {0} items.", batchCount);
                            }
                            break;
                        case ReceiverState.WaitingForStringHeader:
                            // Receive a single integer, indicating the length of the string
                            if (data.Length == sizeof(int))
                            {
                                inProgressLabelLength = BitConverter.ToInt32(data, 0);
                                recState = ReceiverState.WaitingForString;
                            }
                            break;
                        case ReceiverState.WaitingForString:
                            // Receive a string of header-defined length
                            if (data.Length == inProgressLabelLength)
                            {
                                inProgressLabel = UTF8Encoding.UTF8.GetString(data);
                                recState = ReceiverState.WaitingForImageHeader;
                            }
                            break;
                        case ReceiverState.WaitingForImageHeader:
                            // Receive two integers, indicating the height and width, respectively, of the image
                            if (data.Length == (2 * sizeof(int)))
                            {
                                int h = BitConverter.ToInt32(data, 0);
                                int w = BitConverter.ToInt32(data, sizeof(int));
                                inProgressExpectedSize = h * w * 3;
                                inProgressImage = new Image<Bgr, byte>(w, h);
                                recState = ReceiverState.WaitingForImage;
                            }
                            break;
                        case ReceiverState.WaitingForImage:
                            // Receive a number of bytes, corresponding to the pixel values of the image
                            if (data.Length == inProgressExpectedSize)
                            {
                                int i = 0;
                                int tr = inProgressImage.Rows;
                                int tc = inProgressImage.Cols;
                                int tch = inProgressImage.NumberOfChannels;

                                for (int c = 0; c < tch; c++)
                                {
                                    Image<Gray, Byte> channel = inProgressImage[c];
                                    byte[, ,] cData = channel.Data;
                                    for (int x = 0; x < tr; x++)
                                    {
                                        for (int y = 0; y < tc; y++)
                                        {
                                            cData[x, y, 0] = data[i++];
                                        }
                                    }
                                    inProgressImage[c] = channel;
                                }
                                rois.Add(new Tuple<string, Image<Bgr, Byte>>(inProgressLabel, inProgressImage.Copy()));
                                // Flush the temporary resources 
                                inProgressLabel = string.Empty;
                                inProgressLabelLength = 0;
                                inProgressExpectedSize = 0;
                                inProgressImage.Dispose();
                                inProgressImage = null;

                                // Check to see if we've received all in the batch
                                if (rois.Count == batchCount)
                                {
                                    Console.WriteLine("Receiver: Received {0} of {1} items, waiting for next batch", rois.Count, batchCount);
                                    // End of the batch! Fire off the event
                                    try
                                    {
                                        if (NewROIsReceived != null)
                                        {
                                            NewROIsReceived(rois);
                                        }
                                        // Free resources in the list of tuples
                                        foreach (Tuple<string, Image<Bgr, Byte>> tup in rois)
                                        {
                                            tup.Item2.Dispose();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Receiver: exception thrown during batch reset procedure: {0}", ex.Message);
                                    }
                                    rois.Clear();
                                    recState = ReceiverState.WaitingForBatchStart;
                                }
                                else
                                {
                                    // More tuples to come; Wait for the next one
                                    Console.WriteLine("Receiver: Received {0} of {1} items, waiting for more", rois.Count, batchCount);
                                    recState = ReceiverState.WaitingForStringHeader;
                                }
                            }
                            break;
                        case ReceiverState.Invalid:
                            // Error!
                            break;
                        default:
                            // Error!
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                // Flush the temporary resources 
                inProgressLabel = string.Empty;
                inProgressLabelLength = 0;
                inProgressExpectedSize = 0;
                inProgressImage.Dispose();
                inProgressImage = null;

                // Reset the ROIs list
                if (rois != null)
                {
                    foreach (Tuple<string, Image<Bgr, Byte>> tup in rois)
                    {
                        tup.Item2.Dispose();
                    }
                    rois.Clear();
                }
                rois = new List<Tuple<string, Image<Bgr, byte>>>();
                recState = ReceiverState.WaitingForBatchStart;
            }
        }

    }
}
