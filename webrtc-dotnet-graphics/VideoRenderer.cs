using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
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
        private readonly DisposableList<VideoFrame> _frames = new DisposableList<VideoFrame>();
        private readonly ConcurrentQueue<long> _queue = new ConcurrentQueue<long>();

        public VideoTrack VideoTrack { get; }

        public int VideoFrameWidth { get; }
        public int VideoFrameHeight { get; }
        public int VideoFrameQueueSize { get; }
        public int MissedFrameCount { get; private set; }

        public DXGI.Factory2 FactoryDXGI { get; }

        public D3D11.Device Device3D { get; }
        public DXGI.Device DeviceDXGI { get; }
        public D3D11.Multithread ThreadLock3D { get; }

        protected VideoRenderer(VideoTrack videoTrack, RendererOptions options)
        {
            VideoTrack = videoTrack;

            VideoFrameWidth = options.VideoFrameWidth;
            VideoFrameHeight = options.VideoFrameHeight;
            VideoFrameQueueSize = options.VideoFrameQueueSize;

            videoTrack.LocalVideoFrameEncoded += OnLocalVideoFrameEncoded;

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
            if (_frames.Count == 0)
            {
                for (int i = 0; i < VideoFrameQueueSize; ++i)
                {
                    _queue.Enqueue(i);
                    _frames.Add(OnCreateFrame());
                }
            }
        }

        protected virtual VideoFrame OnCreateFrame()
        {
            return new VideoFrame(this);
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
                    videoTrack.LocalVideoFrameEncoded -= OnLocalVideoFrameEncoded;

                _frames?.Dispose();

                ThreadLock3D?.Dispose();
                DeviceDXGI?.Dispose();
                Device3D?.Dispose();
                FactoryDXGI?.Dispose();
            }
        }

        /// <summary>
        /// Acquires the next frame for rendering and sending under a D3D11 thread-lock.
        /// If no frame is ready, returns an empty acquired-frame.
        /// </summary>
        /// <remarks>
        /// The result must be disposed when done.
        /// This will send the frame to the webrtc video-track.
        /// </remarks>
        protected MaybeFrame AcquireNextFrame()
        {
            // TODO: Using a delegate to draw the frame allows capturing parameters,
            // but creates a new object every time, so is not GC friendly...
            if (IsDisposed)
                return default;

            EnsureVideoFrames();

            if (_queue.TryDequeue(out long frameId))
            {
                int index = (int)frameId;
                var frame = _frames[index];
                ThreadLock3D.Enter();
                return new MaybeFrame(this, frame, frameId);
            }

            OnMissedFrame();
            return default;
        }

        internal void Release(in MaybeFrame af)
        {
            if (af.Frame != null)
            {
                ThreadLock3D.Leave();
                af.Frame.Send(VideoTrack, af.FrameId);
            }
        }

        protected virtual void OnMissedFrame()
        {
            MissedFrameCount += 1;
        }

        protected virtual void OnLocalVideoFrameEncoded(PeerConnection pc, int trackId, long frameId, IntPtr rgbaPixels)
        {
            if (IsDisposed)
                return;

            // Put the texture back in the queue.
            Debug.Assert(!_queue.Contains(frameId));
            _queue.Enqueue(frameId);
        }
    }
}
