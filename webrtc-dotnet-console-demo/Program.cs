using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WonderMediaProductions.WebRtc;
using static System.Console;

namespace VideoGeneratorServer
{
    class Program
    {
        static unsafe void Main(string[] args)
        {

            try
            {
                // For debugging, run everything on this thread. 
                // Should never be done in production.
                // Note that webrtc callbacks are done on the signaling thread, and must return asap.
                // SimplePeerConnection.InitializeThreading(options => options.UseWorkerThread = options.UseSignalingThread = false);

                OutputEncoding = Encoding.UTF8;

                int frameIndex = 0;

                using (var senderOutgoingMessages = new ReplaySubject<DataMessage>())
                using (var sender = new ObservablePeerConnection("Sender", options => { }))
                using (var receiver = new ObservablePeerConnection("Receiver", options => { options.CanReceiveVideo = true; }))
                using (var background = Image.Load<Argb32>("background-small.jpg"))
                using (receiver.ReceivedVideoStream.Buffer(2).Subscribe(frames =>
                {
                    var frame0 = frames[0];

                    // Save as JPEG for debugging. SLOW!
                    if (frame0.Width == frame0.StrideY)
                    {
                        var span = new ReadOnlySpan<byte>(frame0.DataY.ToPointer(), frame0.Width * frame0.Height);
                        using (var image = Image.LoadPixelData<Y8>(span, frame0.Width, frame0.Height))
                        {
                            image.Save($@"frame_{frameIndex:D000000}.bmp");
                        }

                        ++frameIndex;
                    }
                    else
                    {
                        WriteLine("Unsupported frame layout");
                    }
                    if (frames.Count == 2)
                    {
                        var frame1 = frames[1];
                        var dt = frame1.TimeStamp - frame0.TimeStamp;
                        WriteLine($"Received video frame\t{frame1.Width}x{frame1.Height}\tΔt={dt.TotalMilliseconds:000.0}\tutc={frame1.TimeStamp:G}");
                    }
                }))
                {
                    senderOutgoingMessages.OnNext(new DataMessage("data", "Hello"));

                    sender.Connect(senderOutgoingMessages, receiver.LocalSessionDescriptionStream, receiver.LocalIceCandidateStream);

                    var receiverOutgoingMessages = receiver
                        .ReceivedDataStream
                        .Where(msg => msg.Content == "Hello")
                        .Select(msg => new DataMessage(msg.Label, "World"));

                    receiver.Connect(receiverOutgoingMessages, sender.LocalSessionDescriptionStream, sender.LocalIceCandidateStream);

                    sender.AddDataChannel("data", DataChannelFlag.None);
                    sender.AddStream(StreamTrack.Video);

                    sender.CreateOffer();

                    WriteLine("Press any key to exit");

                    var timeout = TimeSpan.FromMilliseconds(1000 / 25.0);
                    while (!KeyAvailable && SimplePeerConnection.PumpQueuedMessages(timeout))
                    {
                        var frame = background.Frames[0];
                        var pixels = MemoryMarshal.Cast<Argb32, uint>(frame.GetPixelSpan());
                        sender.SendVideoFrame(
                            MemoryMarshal.GetReference(pixels), 
                            frame.Width * 4, 
                            frame.Width, 
                            frame.Height, 
                            VideoFrameFormat.Argb32);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine($"*** FAILURE: {ex}");
            }

            ReadLine();
        }
    }
}
