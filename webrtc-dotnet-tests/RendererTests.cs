using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WonderMediaProductions.WebRtc.GraphicsD3D11;

namespace WonderMediaProductions.WebRtc
{
    [TestClass]
    public class RendererTests
    {
        // TODO: Can't get this test running with async/await, gets stuck while disposing, deadlocks
        [TestMethod]
        public void RendersAndSendsFrameUsingD3D11()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool hasNvEnc = PeerConnection.SupportsHardwareTextureEncoding;

            if (isWindows && hasNvEnc)
            {
                PeerConnection.Configure(new GlobalOptions
                {
                    UseFakeDecoders = true,
                    LogToDebugOutput = false,
                    MinimumLogLevel = TraceLevel.Info
                });

                using (var sender = new ObservablePeerConnection(new PeerConnectionOptions()))
                using (var receiver = new ObservablePeerConnection(new PeerConnectionOptions { CanReceiveVideo = true }))
                using (var vt = new ObservableVideoTrack(sender, VideoEncoderOptions.OptimizedFor(320, 240, 10)))
                {
                    using (var rnd = new VideoRenderer(vt, new RendererOptions { VideoFrameQueueSize = 2 }))
                    {
                        // Wait until sender and receiver are connected,
                        // signaling is complete, 
                        // and video track is added.

                        // TODO: When using tasks for this, this test hangs when disposing!

                        // ReSharper disable once InvokeAsExtensionMethod
                        //var ready = Observable.Zip(
                        //    receiver.ConnectionStateStream.FirstAsync(s => s == ConnectionState.Connected), 
                        //    sender.ConnectionStateStream.FirstAsync(s => s == ConnectionState.Connected), 
                        //    receiver.SignalingStateStream.FirstAsync(s => s == SignalingState.Stable), 
                        //    sender.SignalingStateStream.FirstAsync(s => s == SignalingState.Stable), 
                        //    receiver.RemoteTrackChangeStream.FirstAsync(
                        //        c => !string.IsNullOrEmpty(c.TransceiverMid) &
                        //             c.MediaKind == TrackMediaKind.Video &&
                        //             c.ChangeKind == TrackChangeKind.Changed), 
                        //    (a, b, c, d, e) => true);
                        //// Wait until connected and video track is ready.
                        //var ev = new AutoResetEvent(false);
                        //ready.Subscribe(_ => ev.Set());

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
                        };

                        // The remote peer connection is not immediately ready to receive frames,
                        // so we keep sending until it succeeds.
                        // TODO: Figure out what webrtc event can be used for this.
                        while (remoteVideoFrameReceivedCount == 0)
                        {
                            using (rnd.TakeNextFrameForSending())
                            {
                            }
                        }

                        // Continue sending until the video queue is empty
                        while (rnd.VideoFrameQueueCount > 0)
                        {
                            using (rnd.TakeNextFrameForSending())
                            {
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
