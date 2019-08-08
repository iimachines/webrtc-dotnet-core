using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace WonderMediaProductions.WebRtc.GraphicsD3D11
{
    /// <summary>
    /// Renders video frames to a webrtc <see cref="VideoTrack"/> using D3D11,
    /// using a queue of frames to allow overlapped rendering and encoding.
    /// </summary>
    public class VideoRenderer : Disposable
    {
        private readonly Dictionary<IntPtr, VideoFrameBuffer> _frameTable = new Dictionary<IntPtr,VideoFrameBuffer>();

        private readonly ConcurrentQueue<IntPtr> _queue = new ConcurrentQueue<IntPtr>();

        private SdlWindow _sdlWindow;

        public VideoTrack VideoTrack { get; }

        public int VideoFrameWidth { get; }
        public int VideoFrameHeight { get; }
        public int VideoFrameQueueSize { get; }

        public int SendFrameCount { get; private set; }
        public int MissedFrameCount { get; private set; }

        public DXGI.Factory2 FactoryDXGI { get; }

        public D3D11.Device Device3D { get; }
        public DXGI.Device DeviceDXGI { get; }
        public D3D11.Multithread ThreadLock3D { get; }

        [CanBeNull]
        public DXGI.SwapChain1 SwapChain { get; }

        public int VideoFrameQueueCount => _queue.Count;

        
        public VideoRenderer(VideoTrack videoTrack, RendererOptions options)
        {
            VideoTrack = videoTrack;

            VideoFrameWidth = options.VideoFrameWidth;
            VideoFrameHeight = options.VideoFrameHeight;
            VideoFrameQueueSize = options.VideoFrameQueueSize;

            videoTrack.LocalVideoFrameProcessed += OnLocalVideoFrameProcessed;

            // _onMissedFrame = options.OnMissedFrame ?? OnMissedFrame;

            bool debug = options.CreationFlags.HasFlag(D3D11.DeviceCreationFlags.Debug);
            FactoryDXGI = new DXGI.Factory2(debug);

            // Find the requested adapter.
            using (var adapters = FactoryDXGI.Adapters.ToDisposableList())
            {
                var adapter = adapters.First(a => a.Description.VendorId == options.AdapterVendorId);

                Device3D = new D3D11.Device(adapter, options.CreationFlags, options.FeatureLevels);

                DeviceDXGI = Device3D.QueryInterface<DXGI.Device>();

                // We need to access D3D11 on multiple threads, so enable multi-threading
                ThreadLock3D = Device3D.ImmediateContext.QueryInterface<D3D11.Multithread>();
                ThreadLock3D.SetMultithreadProtected(true);

                if (options.PreviewWindowOptions != null)
                {
                    var width = options.PreviewWindowOptions.Width ?? VideoFrameWidth;
                    var height = options.PreviewWindowOptions.Height ?? width * VideoFrameHeight / VideoFrameWidth;

                    _sdlWindow = new SdlWindow("WebRTC server preview", width, height);


                    // SwapChain description
                    var desc = new DXGI.SwapChainDescription1()
                    {
                        BufferCount = 2,
                        AlphaMode = DXGI.AlphaMode.Unspecified,
                        Format = DXGI.Format.B8G8R8A8_UNorm,
                        Width = VideoFrameWidth,
                        Height = VideoFrameHeight,
                        Scaling = DXGI.Scaling.Stretch,
                        Stereo = false,
                        Flags = DXGI.SwapChainFlags.AllowTearing| DXGI.SwapChainFlags.FrameLatencyWaitAbleObject,
                        Usage = DXGI.Usage.RenderTargetOutput,
                        SampleDescription = new DXGI.SampleDescription(1, 0),
                        SwapEffect = DXGI.SwapEffect.FlipDiscard,
                    };

                    SwapChain = new DXGI.SwapChain1(FactoryDXGI, Device3D, _sdlWindow.NativeHandle, ref  desc);

                    using (var swapChain2 = SwapChain.QueryInterface<DXGI.SwapChain2>())
                    {
                        var value = swapChain2.MaximumFrameLatency;
                        swapChain2.MaximumFrameLatency = 1;
                    }
                }
            }
        }

        /// <summary>
        /// This method can be called multiple times,
        /// it will ensure that the video frames are created and queued.
        /// 
        /// This method is called automatically when acquiring the very first frame,
        /// but you can also call it right after the constructor to pre-allocate the frames.
        /// </summary>
        public void EnsureVideoFrames()
        {
            if (_frameTable.Count == 0)
            {
                for (int i = 0; i < VideoFrameQueueSize; ++i)
                {
                    var frame = OnCreateFrame();
                    _frameTable.Add(frame.Texture.NativePointer, frame);
                    _queue.Enqueue(frame.Texture.NativePointer);
                }
            }
        }

        protected virtual VideoFrameBuffer OnCreateFrame()
        {
            return new VideoFrameBuffer(this);
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (_queue.Count != VideoFrameQueueSize)
                {
                    Debug.WriteLine("VideoRenderer is being disposed while frames are still being encoded.");
                }

                var videoTrack = VideoTrack;
                if (videoTrack != null)
                    videoTrack.LocalVideoFrameProcessed -= OnLocalVideoFrameProcessed;

                _frameTable.Values.DisposeAll();

                ThreadLock3D?.Dispose();
                DeviceDXGI?.Dispose();
                Device3D?.Dispose();
                FactoryDXGI?.Dispose();

                _sdlWindow?.Dispose();
            }
        }

        /// <summary>
        /// Tries to dequeue the next frame for rendering under a D3D11 thread-lock.
        /// If no frame is ready, returns an empty <see cref="MaybeSendableFrame"/>
        /// After you have rendered to the frame's texture, call dispose on the <see cref="MaybeSendableFrame"/> to send it.
        /// </summary>
        /// <remarks>
        /// The result must be disposed when done.
        /// This will send the frame to the webrtc video-track.
        /// </remarks>
        public MaybeSendableFrame TakeNextFrameForSending()
        {
            // TODO: Using a delegate to draw the frame allows capturing parameters,
            // but creates a new object every time, so is not GC friendly...
            if (IsDisposed)
                return default;

            EnsureVideoFrames();

            if (_queue.TryDequeue(out var texturePtr))
            {
                ThreadLock3D.Enter();
                var frame = _frameTable[texturePtr];

                if (frame != null && SwapChain != null)
                {
                    var backBuffer = D3D11.Resource.FromSwapChain<D3D11.Texture2D>(SwapChain, 0);
                    Device3D.ImmediateContext.CopyResource(frame.Texture, backBuffer);
                    SwapChain.Present(0, DXGI.PresentFlags.DoNotWait | DXGI.PresentFlags.AllowTearing);
                    _sdlWindow.PollAllPendingEvents();
                }

                return new MaybeSendableFrame(this, frame);
            }

            OnMissedFrame();
            return default;
        }

        internal void TransmitSendableFrame(in MaybeSendableFrame af)
        {
            var frame = af.Frame;
            if (frame != null)
            {
                ThreadLock3D.Leave();
                frame.Send(VideoTrack);
                OnFrameSend(frame);
            }
        }

        protected virtual void OnFrameSend(VideoFrameBuffer frame)
        {
            SendFrameCount += 1;
        }

        protected virtual void OnMissedFrame()
        {
            MissedFrameCount += 1;
        }

        protected virtual void OnLocalVideoFrameProcessed(PeerConnection pc, int trackId, IntPtr texturePtr, bool isEncoded)
        {
            if (IsDisposed)
                return;

            if (!isEncoded)
            {
                // Console.WriteLine($"WARNING: A video frame was not encoded!");
            }

            // Put the texture back in the queue.
            Debug.Assert(!_queue.Contains(texturePtr));

            _queue.Enqueue(texturePtr);
        }
    }
}
