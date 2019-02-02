using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using SharpDX.IO;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using WIC = SharpDX.WIC;
using DWrite = SharpDX.DirectWrite;

namespace WonderMediaProductions.WebRtc
{
    public class D3D11Renderer : Disposable, IRenderer
    {
        private const int NVidiaVendorId = 4318;

        private const int VideoFrameQueueSize = 3;

        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private readonly string _backgroundPath;

        private readonly DXGI.Factory2 _factoryDxgi;

        private readonly D3D11.Device _device3D;
        private readonly D3D11.DeviceContext _context3D;
        private readonly DXGI.Device _deviceDxgi;

        private readonly DWrite.Factory _factoryDWrite;
        private readonly WIC.ImagingFactory2 _factoryWic;

        private readonly D2D1.Device _device2D;
        private readonly D2D1.DeviceContext _context2D;
        private readonly D2D1.Factory _factory2D;

        private readonly D2D1.Bitmap1 _backgroundBitmap;
        private readonly D2D1.Ellipse _ballEllipse;
        private readonly D2D1.Brush _ballBrush;

        private readonly D3D11.Multithread _threadLock;

        private readonly DisposableList<D2D1.Bitmap1> _renderBitmaps = new DisposableList<D2D1.Bitmap1>();
        private readonly DisposableList<D3D11.Texture2D> _renderTargets = new DisposableList<D3D11.Texture2D>();

        private readonly ConcurrentQueue<long> _renderQueue = new ConcurrentQueue<long>();

        public D3D11Renderer(int frameWidth, int frameHeight, ObservableVideoTrack videoTrack, bool debug)
        {
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            VideoTrack = videoTrack;

            _subscriptions.Add(videoTrack.LocalVideoFrameEncodedStream.Subscribe(OnLocalVideoFrameEncoded));

            _backgroundPath = "background-small.jpg";

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
                _context3D = new D3D11.DeviceContext(_device3D.ImmediateContext.NativePointer);

                _deviceDxgi = _device3D.QueryInterface<DXGI.Device>();

                // We need to access D3D11 on multiple threads, so enable multi-threading
                _threadLock = _device3D.ImmediateContext.QueryInterface<D3D11.Multithread>();
                _threadLock.SetMultithreadProtected(true);

                _factoryDWrite = new DWrite.Factory(DWrite.FactoryType.Shared);
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
            using (var inputStream = new WIC.WICStream(_factoryWic, _backgroundPath, NativeFileAccess.Read))
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

            // Create render target queue
            var targetDescription = new D3D11.Texture2DDescription()
            {
                Width = FrameWidth,
                Height = FrameHeight,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                BindFlags = D3D11.BindFlags.RenderTarget,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                OptionFlags = D3D11.ResourceOptionFlags.None,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                Usage = D3D11.ResourceUsage.Default
            };

            for (int i = 0; i < VideoFrameQueueSize; ++i)
            {
                _renderQueue.Enqueue(i);

                var renderTarget = new D3D11.Texture2D(_device3D, targetDescription);
                _renderTargets.Add(renderTarget);

                using (var surface = renderTarget.QueryInterface<DXGI.Surface>())
                {
                    var renderBitmap = new D2D1.Bitmap1(_context2D, surface);
                    _renderBitmaps.Add(renderBitmap);
                }
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
                DisposeAllFields();
            }
        }

        public void SendFrame(TimeSpan elapsedTime, int frameIndex)
        {
            // TODO: Draw bouncing ball.
            var a = Math.PI * elapsedTime.TotalSeconds;
            var h = FrameHeight - _ballEllipse.RadiusY;
            var y = (float)(FrameHeight - Math.Abs(Math.Sin(a) * h));

            if (_renderQueue.TryDequeue(out long frameId))
            {
                int index = (int)frameId;

                _threadLock.Enter();
                _context2D.Target = _renderBitmaps[index];
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

                var target = _renderTargets[index];
                VideoTrack.SendVideoFrame(frameId, target.NativePointer, FrameWidth * 4, FrameWidth, FrameHeight, VideoFrameFormat.GpuTextureD3D11);
            }
            else
            {
                Console.WriteLine("WARNING: All D3D11 textures are used by the H264 encoder!");
            }
        }

        private void OnLocalVideoFrameEncoded(VideoFrameMessage message)
        {
            // Put the texture back in the queue
            // Console.WriteLine($"Encoded {message.FrameId}");
            _renderQueue.Enqueue(message.FrameId);
        }
    }
}
