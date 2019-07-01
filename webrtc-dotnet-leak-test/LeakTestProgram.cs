using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using SixLabors.ImageSharp;
using WonderMediaProductions.WebRtc.GraphicsD3D11;

namespace WonderMediaProductions.WebRtc
{

    public class LeakTestProgram
    {
        // TODO: Can't get this test running with async/await, gets stuck while disposing, deadlocks

        public static void Main()
        {
            PeerConnection.Configure(new GlobalOptions
            {
                UseFakeDecoders = true,
                LogToDebugOutput = false,
                MinimumLogLevel = TraceLevel.Info
            });

            while (true)
            {
                Render();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Console.WriteLine("Press ENTER");
                Console.ReadLine();
            }
        }

        private static unsafe void Render()
        {
            const int frameWidth = 2560;
            const int frameHeight = 1440;
            const int frameRate = 60;

            // var options = VideoEncoderOptions.OptimizedFor(frameWidth, frameHeight, frameRate);
            var options = new VideoEncoderOptions
            {
                MaxBitsPerSecond = 12_000_000,
                MinBitsPerSecond = 10_000_000,
                MaxFramesPerSecond = frameRate
            };

            using (var sender = new ObservablePeerConnection(new PeerConnectionOptions()))
            using (var receiver = new ObservablePeerConnection(new PeerConnectionOptions { CanReceiveVideo = true }))
            {
                using (var vt = new ObservableVideoTrack(sender, options))
                {
                    using (var rnd = new BouncingBallRenderer(vt, 10, new RendererOptions
                    {
                        VideoFrameWidth = frameWidth,
                        VideoFrameHeight = frameHeight,
                        VideoFrameQueueSize = 2
                    }))
                    {
                        receiver.Connect(
                            Observable.Never<DataMessage>(),
                            sender.LocalSessionDescriptionStream,
                            sender.LocalIceCandidateStream);

                        sender.Connect(
                            Observable.Never<DataMessage>(),
                            receiver.LocalSessionDescriptionStream,
                            receiver.LocalIceCandidateStream);

                        sender.CreateOffer();

                        int remoteVideoFrameReceivedCount = 0;

                        receiver.RemoteVideoFrameReceived += (pc, frame) =>
                        {
                            remoteVideoFrameReceivedCount += 1;

                            // Save as JPEG for debugging. SLOW!
                            // TODO: Doesn't work yet, H264 decoding not yet supported, only VP8
                            //if (frame is VideoFrameYuvAlpha yuvFrame && yuvFrame.Width == yuvFrame.StrideY)
                            //{
                            //    var span = new ReadOnlySpan<byte>(yuvFrame.DataY.ToPointer(), yuvFrame.Width * yuvFrame.Height);
                            //    using (var image = Image.LoadPixelData<Y8>(span, yuvFrame.Width, yuvFrame.Height))
                            //    {
                            //        image.Save($@"frame_{remoteVideoFrameReceivedCount:D000000}.bmp");
                            //    }
                            //}
                        };

                        using (var clock = new PreciseWaitableClock(EventResetMode.AutoReset))
                        {
                            var startTime = clock.GetCurrentTime().AddSeconds(1);

                            var nextTime = startTime;
                            // The remote peer connection is not immediately ready to receive frames,
                            // so we keep sending until it succeeds.
                            // TODO: Figure out what webrtc event can be used for this.
                            while (!Console.KeyAvailable)
                            {
                                clock.SetFutureEventTime(nextTime);

                                clock.WaitHandle.WaitOne();

                                var elapsedTime = clock.GetCurrentTime() - startTime;
                                rnd.SendFrame(elapsedTime);

                                nextTime = nextTime.AddSeconds(1.0 / frameRate);
                            }
                        }
                    }

                    // The video renderer is now disposed while the video track is still encoding some textures
                    // This should not crash.
                    // We need to wait a while before disposing the video-track and peer-connection to check this.
                    Thread.Sleep(100);
                }
            }
        }
    }
}
