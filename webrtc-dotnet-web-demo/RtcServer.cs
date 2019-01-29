using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;
using WonderMediaProductions.WebRtc;
using PixelColor = SixLabors.ImageSharp.PixelFormats.Bgra32;

namespace webrtc_dotnet_demo
{
    public static class RtcServer
    {
        private static void VideoRenderer(object parameter)
        {
            try
            {
                var pc = (ObservablePeerConnection)parameter;

                using (var background = Image.Load<PixelColor>("background-small.jpg"))
                {
                    background.Mutate(ctx => ctx.Resize(320, 240));

                    // Pre-created bouncing ball frames.
                    // ImageSharp is not that fast yet, and our goal is to benchmark webrtc and NvEnc, not ImageSharp.

                    var ballRadius = background.Width / 20f;
                    var ballPath = new EllipsePolygon(0, 0, ballRadius);
                    var ballColor = new PixelColor(255, 255, 128);

                    const int frameCount = 60;
                    const int framesPerSecond = 60;

                    var videoFrames = Enumerable
                        .Range(0, frameCount)
                        .Select(i =>
                        {
                            var image = background.Clone();

                            var a = Math.PI * i / frameCount;
                            var h = image.Height - ballRadius;
                            var y = image.Height - (float)(Math.Abs(Math.Sin(a) * h));

                            image.Mutate(ctx => ctx
                                .Fill(GraphicsOptions.Default, ballColor, ballPath.Translate(image.Width/2f, y)));

                            return image;
                        })
                        .ToArray();

                    using (new CompositeDisposable(videoFrames.Cast<IDisposable>()))
                    {
                        TimeSpan startTime = TimeSpan.Zero;
                        TimeSpan nextFrameTime = TimeSpan.Zero;

                        long nextFrameIndex = 0;

                        var sw = new Stopwatch();

                        while (Thread.CurrentThread.IsAlive && !pc.IsDisposed)
                        {
                            if (pc.SignalingState == SignalingState.Stable)
                            {
                                var currentTime = SimplePeerConnection.GetRealtimeClockTimeInMicroseconds();

                                if (startTime == TimeSpan.Zero)
                                {
                                    startTime = currentTime;
                                    sw.Start();
                                }

                                if (currentTime >= nextFrameTime)
                                {
                                    Console.Write($"{sw.ElapsedMilliseconds:D06}\t");
                                    sw.Restart();

                                    var frameIndex = (currentTime - startTime).Ticks * framesPerSecond / TimeSpan.TicksPerSecond;

                                    var skippedFrameCount = frameIndex - nextFrameIndex;
                                    Debug.Assert(skippedFrameCount >= 0);

                                    if (skippedFrameCount >= 1)
                                    {
                                        Console.WriteLine($"Skipped {skippedFrameCount} frames!");
                                    }

                                    var imageFrame = videoFrames[frameIndex % frameCount].Frames[0];
                                    var pixels = MemoryMarshal.Cast<PixelColor, uint>(imageFrame.GetPixelSpan());

                                    pc.SendVideoFrame(
                                        MemoryMarshal.GetReference(pixels),
                                        imageFrame.Width * 4,
                                        imageFrame.Width,
                                        imageFrame.Height,
                                        VideoFrameFormat.CpuTexture);

                                    nextFrameIndex = frameIndex + 1;

                                    // TODO: Use Math.DivRem and take remainder into account?
                                    // TODO: Should get feedback from connected peer about frame-rate and resolution.
                                    nextFrameTime =
                                        startTime + TimeSpan.FromTicks(
                                            nextFrameIndex * TimeSpan.TicksPerSecond / framesPerSecond);
                                }
                                else
                                {
                                    // TODO: Use Win32 waitable timers, or expose webrtc's high-precision (?) TaskQueue
                                    Thread.Sleep(0);
                                }
                            }
                            else
                            {
                                // Wait until peer connection is stable before sending frames.
                                Thread.Sleep(500);
                            }
                        }
                    }
                }
            }

            catch (ThreadInterruptedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task Run(WebSocket ws)
        {
            var receiveBuffer = new byte[1024 * 4];
            var renderThread = new Thread(VideoRenderer);

            SimplePeerConnection.Configure(options => options.IsSingleThreaded = true);

            using (var pc = new ObservablePeerConnection("server", options => { }))
            using (pc.LocalIceCandidateStream.Subscribe(ice => ws.SendJsonAsync("ice", ice)))
            using (pc.LocalSessionDescriptionStream.Subscribe(sd => ws.SendJsonAsync("sdp", sd)))
            {
                renderThread.Start(pc);

                var msgStream = Observable.Never<DataMessage>();
                var iceStream = new Subject<IceCandidate>();
                var sdpStream = new Subject<SessionDescription>();

                pc.Connect(msgStream, sdpStream, iceStream);

                pc.AddDataChannel("data", DataChannelFlag.None);
                pc.AddStream(StreamTrack.Video);

                pc.CreateOffer();


                while (!ws.CloseStatus.HasValue)
                {
                    var message = await ws.ReceiveJsonAsync(default, receiveBuffer);
                    if (message == null)
                        break;

                    var payload = message["payload"];

                    if (payload.Any())
                    {
                        switch (message["action"].Value<string>())
                        {
                            case "ice":
                            {
                                iceStream.OnNext(new IceCandidate(payload));
                                break;
                            }

                            case "sdp":
                            {
                                sdpStream.OnNext(new SessionDescription(payload));
                                break;
                            }
                        }
                    }
                }

                Console.WriteLine("Websocket was closed by client");

                renderThread.Interrupt();
                renderThread.Join();
            }
        }
    }
}
