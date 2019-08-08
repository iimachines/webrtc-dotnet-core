using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using SharpDX.Mathematics.Interop;
using WonderMediaProductions.WebRtc.GraphicsD3D11;

namespace WonderMediaProductions.WebRtc
{
    public static class RtcRenderingServer
    {
        const int VideoFrameWidth = 1920*2;
        const int VideoFrameHeight = 1080*2;
        const int VideoFrameRate = 60;

        private static IRenderer CreateRenderer(ObservableVideoTrack videoTrack, ILogger logger)
        {
            PeerConnection.Configure(new GlobalOptions
            {
                MinimumLogLevel = TraceLevel.Info
            });

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool supportsNvEnc = PeerConnection.SupportsHardwareTextureEncoding;

            // TODO: Add support for OpenGL, and test it.
            // Maybe use https://github.com/mellinoe/veldrid
            return isWindows && supportsNvEnc
                ? (IRenderer)new BouncingBallRenderer(videoTrack, 
                    8,
                    new BoundingBallOptions
                    {
                        VideoFrameWidth = VideoFrameWidth,
                        VideoFrameHeight = VideoFrameHeight,
                        PreviewWindowOptions = new PreviewWindowOptions
                            {
                                Width = 1920/2
                            },

                        TimeRulerOptions = new TimeRulerOptions()
                    })
                : new ImageSharpRenderer(VideoFrameWidth, VideoFrameHeight, videoTrack);
        }

        class SharedState : IDisposable
        {
            public readonly ILogger Logger;

            public readonly ObservableVideoTrack VideoTrack;

            public readonly ConcurrentQueue<MouseMessage> MouseMessageQueue = new ConcurrentQueue<MouseMessage>();

            public SharedState(ObservableVideoTrack videoTrack, ILogger logger)
            {
                VideoTrack = videoTrack;
                Logger = logger;
            }

            public void Dispose()
            {
                VideoTrack?.Dispose();
            }
        }

        private static void VideoRenderer(object parameter)
        {
            var state = (SharedState)parameter;
            var videoTrack = state.VideoTrack;
            var peerConnection = videoTrack.PeerConnection;
            var logger = state.Logger;

            try
            {
	            DateTime startTime = default;

	            int frameIndex = 0;

                // Create swap-chain for displaying rendered images on the server


                // var sw = new Stopwatch();
                using (var clock = new PreciseWaitableClock(EventResetMode.AutoReset))
                using (var renderer = CreateRenderer(videoTrack, logger))
                {
                    while (Thread.CurrentThread.IsAlive && !peerConnection.IsDisposed)
                    {
                        if (peerConnection.SignalingState == SignalingState.Stable)
                        {
                            if (frameIndex == 0)
                            {
                                startTime = clock.GetCurrentTime();
                            }

                            MouseMessage lastMouseMessage = null;

                            while (state.MouseMessageQueue.TryDequeue(out var mouseMessage))
                            {
                                lastMouseMessage = mouseMessage;
                            }

                            if (lastMouseMessage != null)
                            {
                                // Render mouse events as quickly as possible.
                                renderer.MousePosition = lastMouseMessage.Kind != MouseEventKind.Up
                                    ? lastMouseMessage.Pos
                                    : (RawVector2?)null;
                            }
                            var elapsedTime = TimeSpan.FromTicks(frameIndex * TimeSpan.TicksPerSecond / videoTrack.FrameRate);
                            renderer.SendFrame(elapsedTime);

                            // Wait until we can render the next frame
                            var currentTime = clock.GetCurrentTime();

                            var skippedFrameCount = 0;
                            for (; ; )
                            {
                                var nextFrameTime = startTime.AddTicks(++frameIndex * TimeSpan.TicksPerSecond / videoTrack.FrameRate);
                                if (nextFrameTime >= currentTime)
                                {
                                    clock.SetFutureEventTime(nextFrameTime);
                                    break;
                                }

                                ++skippedFrameCount;
                            }

                            if (skippedFrameCount > 0)
                            {
                                logger.LogWarning($"Skipped {skippedFrameCount} frames!");
                            }

                            // Wait for the next frame.
                            clock.WaitHandle.WaitOne();
                        }
                        else
                        {
                            // Wait until peer connection is stable before sending frames.
                            Thread.Sleep(500);
                        }
                    }
                }
            }

            catch (ThreadInterruptedException)
            {
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RtcRendererServer thread");
            }
        }

        public static async Task Run(WebSocket ws, CancellationToken cancellation, ILogger logger)
        {
	        T ToObject<T>(JToken token)
	        {
		        var obj = token.ToObject<T>();
		        //var json = JToken.FromObject(obj);
		        //Debug.Assert(JToken.DeepEquals(token, json));
				return obj;
	        }

	        var renderThread = new Thread(VideoRenderer);

            // PeerConnection.Configure(options => options.IsSingleThreaded = true);

            using (var pc = new ObservablePeerConnection(new PeerConnectionOptions
            {
                Name = "WebRTC Server",
                IceServers = { "stun:stun.l.google.com:19302" }
            }))
            using (pc.LocalIceCandidateStream.Subscribe(ice => ws.SendJsonAsync("ice", ice, cancellation)))
            using (pc.LocalSessionDescriptionStream.Subscribe(sd => ws.SendJsonAsync("sdp", sd, cancellation)))
            using (var videoTrack = new ObservableVideoTrack(pc,
                VideoEncoderOptions.OptimizedFor(VideoFrameWidth, VideoFrameHeight, VideoFrameRate, VideoMotion.Medium, VideoMotion.High)))
            {
                var msgStream = Observable.Never<DataMessage>();
                var iceStream = new Subject<IceCandidate>();
                var sdpStream = new Subject<SessionDescription>();

                var sharedState = new SharedState(videoTrack, logger);
                renderThread.Start(sharedState);

                pc.Connect(msgStream, sdpStream, iceStream);

                pc.AddDataChannel(new DataChannelOptions());

                pc.CreateOffer();

                var reader = new WebSocketReader(ws, cancellation);

                while (reader.CanRead && !cancellation.IsCancellationRequested)
                {
                    var message = await reader.ReadJsonAsync();
                    if (message == null)
                        break;

                    var payload = message["payload"];

                    if (payload.Any())
                    {
                        switch (message["action"].Value<string>())
                        {
                            case "ice":
                            {
	                            iceStream.OnNext(ToObject<IceCandidate>(payload));
                                break;
                            }

                            case "sdp":
                            {
                                sdpStream.OnNext(ToObject<SessionDescription>(payload));
                                break;
                            }

                            case "pos":
                            {
                                sharedState.MouseMessageQueue.Enqueue(ToObject<MouseMessage>(payload));
                                break;
                            }
                        }
                    }
                }

                logger.LogInformation(ws.CloseStatus.HasValue ? "Websocket was closed by client" : "Application is stopping...");

                renderThread.Interrupt();
                renderThread.Join();
            }
        }
    }
}
