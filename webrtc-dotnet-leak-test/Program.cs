using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using WonderMediaProductions.WebRtc.GraphicsD3D11;

namespace WonderMediaProductions.WebRtc
{

	public class Program
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

		private static void Render()
		{
			using (var sender = new ObservablePeerConnection(new PeerConnectionOptions()))
			using (var receiver = new ObservablePeerConnection(new PeerConnectionOptions { CanReceiveVideo = true }))
			using (var vt = new ObservableVideoTrack(sender, VideoEncoderOptions.OptimizedFor(320, 240, 10)))
			{
				using (var rnd = new VideoRenderer(vt, new RendererOptions { VideoFrameQueueSize = 2 }))
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
