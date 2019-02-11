using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace WonderMediaProductions.WebRtc
{

    // TODO: This demo doesn't work anymore, because receiving video on the server side is not supported for now.
    class ConsoleDemoProgram
    {
        int remoteFrameIndex = 0;

        static void Main(string[] args)
        {
            var program = new ConsoleDemoProgram();
            program.Run();
        }

        private void Run()
        {
            try
            {
                // For debugging, run everything on this thread. 
                // Should never be done in production.
                // Note that webrtc callbacks are done on the signaling thread, and must return asap.
                PeerConnection.Configure(new GlobalOptions
                {
                    UseWorkerThread = false,
                    UseSignalingThread = false,
                    ForceSoftwareVideoEncoder = true,
                    MinimumLogLevel = System.Diagnostics.TraceLevel.Error,
                    LogToStandardError = false
                });

                PeerConnection.MessageLogged += (message, severity) =>
                {
                    Console.WriteLine($"webrtc [{severity:G}]:\t{message}");
                };

                Console.OutputEncoding = Encoding.UTF8;

                const int frameWidth = 320;
                const int frameHeight = 180;
                const int frameRate = 10;

                using (var senderOutgoingMessages = new ReplaySubject<DataMessage>())
                using (var sender = new ObservablePeerConnection(new PeerConnectionOptions
                {
                    Name = "Sender"
                }))
                using (var receiver = new ObservablePeerConnection(new PeerConnectionOptions
                {
                    Name = "Receiver",
                    CanReceiveVideo = true
                }))
                using (var background = Image.Load<Argb32>("background-small.jpg"))
                using (receiver.ReceivedVideoStream.Buffer(2).Subscribe(SaveFrame))
                using (var imageFrame = new Image<Argb32>(frameWidth, frameHeight))
                using (var videoTrack = new VideoTrack(sender, 
                    VideoEncoderOptions.OptimizedFor(frameWidth, frameHeight, frameRate)))
                {
                    background.Mutate(ctx => ctx.Resize(frameWidth, frameHeight));

                    senderOutgoingMessages.OnNext(new DataMessage("data", "Hello"));

                    sender.Connect(senderOutgoingMessages, receiver.LocalSessionDescriptionStream, receiver.LocalIceCandidateStream);

                    var receiverOutgoingMessages = receiver
                        .ReceivedDataStream
                        .Where(msg => msg.AsText == "Hello")
                        .Do(msg => Console.WriteLine($"Received message {msg.AsText}"))
                        .Select(msg => new DataMessage(msg.Label, "World"));

                    receiver.Connect(receiverOutgoingMessages, sender.LocalSessionDescriptionStream, sender.LocalIceCandidateStream);

                    sender.AddDataChannel(new DataChannelOptions());

                    sender.CreateOffer();

                    Console.WriteLine("Press any key to exit");

                    int localFrameIndex = 0;

                    var timeout = TimeSpan.FromMilliseconds(1000.0 / frameRate);
                    while (!Console.KeyAvailable && PeerConnection.PumpQueuedMessages(timeout))
                    {
                        var frame = imageFrame.Frames[0];
                        var pixels = MemoryMarshal.Cast<Argb32, uint>(frame.GetPixelSpan());
                        videoTrack.SendVideoFrame(
                            localFrameIndex,
                            MemoryMarshal.GetReference(pixels),
                            frame.Width * 4,
                            frame.Width,
                            frame.Height,
                            VideoFrameFormat.Argb32);

                        imageFrame.Mutate(ctx => ctx.DrawImage(GraphicsOptions.Default, background).Rotate(localFrameIndex * 10).Crop(frameWidth, frameHeight));

                        ++localFrameIndex;
                    }

                    sender.RemoveDataChannel("data");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** FAILURE: {ex}");
            }

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        private unsafe void SaveFrame(IList<VideoFrameYuvAlpha> frames)
        {
            var frame0 = frames[0];

            // Save as JPEG for debugging. SLOW!
            if (frame0.Width == frame0.StrideY)
            {
                var span = new ReadOnlySpan<byte>(frame0.DataY.ToPointer(), frame0.Width * frame0.Height);
                using (var image = Image.LoadPixelData<Y8>(span, frame0.Width, frame0.Height))
                {
                    image.Save($@"frame_{remoteFrameIndex:D000000}.bmp");
                }

                ++remoteFrameIndex;
            }
            else
            {
                Console.WriteLine("Unsupported frame layout");
            }

            if (frames.Count == 2)
            {
                var frame1 = frames[1];
                var dt = frame1.TimeStamp - frame0.TimeStamp;
                Console.WriteLine($"Received video frame\t{frame1.Width}x{frame1.Height}\tΔt={dt.TotalMilliseconds:000.0}\tutc={frame1.TimeStamp:G}");
            }
        }
    }
}
