using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;
using SharpDX;
using SharpDX.IO;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using WIC = SharpDX.WIC;

namespace WonderMediaProductions.WebRtc
{
    public class D3D11Renderer : Disposable, IRenderer
    {
        private readonly ILogger _logger;
        private const int NVidiaVendorId = 4318;

        private const int VideoFrameQueueSize = 3;

        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        private readonly DXGI.Factory2 _factoryDxgi;

        private readonly D3D11.Device _device3D;
        // private readonly D3D11.DeviceContext _context3D;
        private readonly DXGI.Device _deviceDxgi;

        // private readonly DWrite.Factory _factoryDWrite;
        private readonly WIC.ImagingFactory2 _factoryWic;

        private readonly D2D1.Device _device2D;
        private readonly D2D1.DeviceContext _context2D;
        private readonly D2D1.Factory _factory2D;

        private readonly D2D1.Bitmap1 _backgroundBitmap;
        private readonly D2D1.Ellipse _ballEllipse;
        private readonly D2D1.Brush _ballBrush;

        private readonly D3D11.Multithread _threadLock;

        private readonly DisposableList<Frame> _frames = new DisposableList<Frame>();

        private readonly ConcurrentQueue<long> _queue = new ConcurrentQueue<long>();

        public D3D11Renderer(int frameWidth, int frameHeight, ObservableVideoTrack videoTrack, bool debug, ILogger logger)
        {
            _logger = logger;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            VideoTrack = videoTrack;

            _subscriptions.Add(videoTrack.LocalVideoFrameEncodedStream.Subscribe(OnLocalVideoFrameEncoded));

            _factoryDxgi = new DXGI.Factory2(debug);

            // We require an NVidia adapter
            using (var adapters = _factoryDxgi.Adapters.ToDisposableList())
            {
                // Try an NVidia adapter first, but we need a device that supports the required feature levels.
                var nvAdapter = adapters.Single(a => a.Description.VendorId == NVidiaVendorId);

                var requiredFeatureLevels3D = new[]
                {
                    D3D.FeatureLevel.Level_11_1
                };

                D3D11.DeviceCreationFlags creationFlags =
                    D3D11.DeviceCreationFlags.BgraSupport |
                    (debug ? D3D11.DeviceCreationFlags.Debug : 0);

                _device3D = new D3D11.Device(nvAdapter, creationFlags, requiredFeatureLevels3D);

                // SharpDX does not increment the reference count when getting the ImmediateContext, unlike all other properties, so we wrap it
                // _context3D = new D3D11.DeviceContext(_device3D.ImmediateContext.NativePointer);

                _deviceDxgi = _device3D.QueryInterface<DXGI.Device>();

                // We need to access D3D11 on multiple threads, so enable multi-threading
                _threadLock = _device3D.ImmediateContext.QueryInterface<D3D11.Multithread>();
                _threadLock.SetMultithreadProtected(true);

                // _factoryDWrite = new DWrite.Factory(DWrite.FactoryType.Shared);
                _factoryWic = new WIC.ImagingFactory2();

                _device2D = new D2D1.Device(_deviceDxgi, new D2D1.CreationProperties
                {
                    DebugLevel = D2D1.DebugLevel.Warning,
                    ThreadingMode = D2D1.ThreadingMode.MultiThreaded,
                    Options = D2D1.DeviceContextOptions.None
                });

                _context2D = new D2D1.DeviceContext(_device2D, D2D1.DeviceContextOptions.None);
            }

            // Load the background image
            using (var decoder = new WIC.JpegBitmapDecoder(_factoryWic))
            using (var inputStream = new WIC.WICStream(_factoryWic, "background-small.jpg", NativeFileAccess.Read))
            using (var formatConverter = new WIC.FormatConverter(_factoryWic))
            using (var bitmapScaler = new WIC.BitmapScaler(_factoryWic))
            {
                decoder.Initialize(inputStream, WIC.DecodeOptions.CacheOnLoad);
                formatConverter.Initialize(decoder.GetFrame(0), WIC.PixelFormat.Format32bppPBGRA);
                bitmapScaler.Initialize(formatConverter, FrameWidth, FrameHeight, WIC.BitmapInterpolationMode.Fant);
                _backgroundBitmap = D2D1.Bitmap1.FromWicBitmap(_context2D, bitmapScaler);
            }

            // Create render target
            _ballEllipse = new D2D1.Ellipse { RadiusX = FrameWidth / 20f, RadiusY = FrameWidth / 20f };

            _ballBrush = new D2D1.SolidColorBrush(_context2D, new RawColor4(1f, 1f, 0f, 1f));

            for (int i = 0; i < VideoFrameQueueSize; ++i)
            {
                _queue.Enqueue(i);
                _frames.Add(new Frame(_device3D, _context2D, frameWidth, frameHeight));
            }
        }


        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public ObservableVideoTrack VideoTrack { get; }
        public RawVector2? BallPosition { get; set; }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (_queue.Count == VideoFrameQueueSize)
                {
                    DisposeGraphics();
                }
            }
        }

        //private static int skip = 0;
        //private static int take = 4;

        private void DisposeGraphics()
        {
            // Enabling the line below fixes the crash when the NvEncoder is destroyed...
            //(_device2D as IUnknown).AddReference();

            //var objs = ComReflection.GetComObjectFields(this)
            //    .OfType<IUnknown>()
            //    .OrderBy(u => u.GetType().Name)
            //    .ToList();

            //objs.Skip(skip).Take(take).ToList().ForEach(u => u.AddReference());

            DisposeAllFields();

            //using (var debug = DXGI.DXGIDebug.TryCreate())
            //using (var context = new D3D11.DeviceContext(_device3D.ImmediateContext.NativePointer))
            //{
            //    context.ClearState();
            //    context.Flush();
            //    context.Dispose();
            //    debug?.ReportLiveObjects(DXGI.DebugId.All, DXGI.DebugRloFlags.Summary);
            //}
        }

        public bool SendFrame(TimeSpan elapsedTime, int frameIndex)
        {
            if (IsDisposed)
                return false;

            // TODO: Draw bouncing ball.
            var a = Math.PI * elapsedTime.TotalSeconds;
            var h = FrameHeight - _ballEllipse.RadiusY;
            var y = (float)(FrameHeight - Math.Abs(Math.Sin(a) * h));

            if (_queue.TryDequeue(out long frameId))
            {
                int index = (int)frameId;

                _threadLock.Enter();

                var frame = _frames[index];

                _context2D.Target = frame.Bitmap;
                _context2D.BeginDraw();
                _context2D.Transform = SharpDX.Matrix3x2.Identity;
                _context2D.DrawBitmap(_backgroundBitmap, new RawRectangleF(0, 0, FrameWidth, FrameHeight), 1, D2D1.BitmapInterpolationMode.NearestNeighbor);

                _context2D.Transform = BallPosition.HasValue
                    ? SharpDX.Matrix3x2.Translation(BallPosition.Value * new SharpDX.Vector2(FrameWidth, FrameHeight))
                    : SharpDX.Matrix3x2.Translation(FrameWidth / 2f, y);

                _context2D.FillEllipse(_ballEllipse, _ballBrush);
                _context2D.EndDraw();
                _context2D.Target = null;
                _threadLock.Leave();

                VideoTrack.SendVideoFrame(frameId, frame.Texture.NativePointer,
                    FrameWidth * 4, FrameWidth, FrameHeight, VideoFrameFormat.GpuTextureD3D11);

                return true;
            }

            _logger.LogWarning("All D3D11 textures are used by the H264 encoder!");
            return false;
        }

        private void OnLocalVideoFrameEncoded(VideoFrameMessage message)
        {
            // Put the texture back in the queue.
            Debug.Assert(!_queue.Contains(message.FrameId));
            _queue.Enqueue(message.FrameId);

            if (IsDisposed && _queue.Count == VideoFrameQueueSize)
            {
                // Dispose the graphics when the last encoded frame has returned.
                DisposeGraphics();
            }
        }

        private sealed class Frame : Disposable
        {
            public readonly D3D11.Device Device3D;
            public readonly D2D1.Bitmap1 Bitmap;
            public readonly D3D11.Texture2D Texture;

            public Frame(D3D11.Device device3D, D2D1.DeviceContext context2D, int width, int height)
            {
                Device3D = new D3D11.Device(device3D.NativePointer);

                // Create render target queue
                var targetDescription = new D3D11.Texture2DDescription()
                {
                    Width = width,
                    Height = height,
                    Format = DXGI.Format.B8G8R8A8_UNorm,
                    ArraySize = 1,
                    MipLevels = 1,
                    BindFlags = D3D11.BindFlags.RenderTarget,
                    SampleDescription = new DXGI.SampleDescription(1, 0),
                    OptionFlags = D3D11.ResourceOptionFlags.None,
                    CpuAccessFlags = D3D11.CpuAccessFlags.None,
                    Usage = D3D11.ResourceUsage.Default
                };

                Texture = new D3D11.Texture2D(device3D, targetDescription);

                using (var surface = Texture.QueryInterface<DXGI.Surface>())
                {
                    Bitmap = new D2D1.Bitmap1(context2D, surface);
                }
            }

            protected override void OnDispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    Bitmap?.Dispose();
                    Texture?.Dispose();
                    Device3D?.Dispose();
                }
            }
        }
    }
}
