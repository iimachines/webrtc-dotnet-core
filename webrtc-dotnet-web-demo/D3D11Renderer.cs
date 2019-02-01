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

        private readonly DXGI.Adapter _adapterDxgi;
        private readonly DXGI.Factory2 _factoryDgxi;
        private readonly DXGI.Factory2 _factoryDgxi2;

        private readonly D2D1.Factory1 _factoryD2D1;
        private readonly DWrite.Factory _factoryDWrite;
        private readonly WIC.ImagingFactory2 _factoryWic;

        private readonly D3D11.Device _device3D;
        private readonly D3D11.Multithread _threadLock;
        private readonly D3D11.DeviceContext4 _context3D;
        private readonly DXGI.Device _deviceDxgi;

        private readonly D2D1.Device _device2D;
        private readonly D2D1.DeviceContext _context2D;

        private readonly D2D1.Bitmap1 _backgroundBitmap;

        private readonly DisposableList<D3D11.Texture2D> _renderTextures = new DisposableList<D3D11.Texture2D>();
        private readonly DisposableList<D2D1.Bitmap1> _renderTargets = new DisposableList<D2D1.Bitmap1>();

        private readonly ConcurrentQueue<long> _renderQueue = new ConcurrentQueue<long>();

        public D3D11Renderer(int frameWidth, int frameHeight, ObservableVideoTrack videoTrack)
        {
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            VideoTrack = videoTrack;

            _subscriptions.Add(videoTrack.LocalVideoFrameEncodedStream.Subscribe(OnLocalVideoFrameEncoded));

            _backgroundPath = "background-small.jpg";

            _factoryDgxi = new DXGI.Factory2(debug: true);

            D3D11.DeviceCreationFlags creationFlags = D3D11.DeviceCreationFlags.BgraSupport | D3D11.DeviceCreationFlags.Debug;

            // We require an NVidia adapter
            using (var adapters = _factoryDgxi.Adapters.ToDisposableList())
            {
                // Try an NVidia adapter first, but we need a device that supports the required feature levels.
                var nvAdapter = adapters.Single(a => a.Description.VendorId == NVidiaVendorId);
                _adapterDxgi = new DXGI.Adapter(nvAdapter.NativePointer);
                _factoryDgxi2 = _adapterDxgi.GetParent<DXGI.Factory2>();

                _factoryD2D1 = new D2D1.Factory1(D2D1.FactoryType.SingleThreaded, D2D1.DebugLevel.Warning);
                _factoryDWrite = new DWrite.Factory(DWrite.FactoryType.Shared);
                _factoryWic = new WIC.ImagingFactory2();

                var requiredFeatureLevels3D = new[] { D3D.FeatureLevel.Level_11_1 };
                _device3D = new D3D11.Device(nvAdapter, creationFlags, requiredFeatureLevels3D);
                _deviceDxgi = _device3D.QueryInterface<DXGI.Device>();

                // We need to access D3D11 on multiple threads, so enable multi-threading
                _threadLock = _device3D.ImmediateContext.QueryInterface<D3D11.Multithread>();
                _threadLock.SetMultithreadProtected(true);

                _context3D = _device3D.ImmediateContext.QueryInterface<D3D11.DeviceContext4>();

                _device2D = new D2D1.Device(_deviceDxgi, new D2D1.CreationProperties
                {
                    DebugLevel = D2D1.DebugLevel.Warning,
                    ThreadingMode = D2D1.ThreadingMode.MultiThreaded,
                    Options = D2D1.DeviceContextOptions.None
                });

                _context2D = new D2D1.DeviceContext(_device2D, D2D1.DeviceContextOptions.None);
            }

            var d2DPixelFormat = new D2D1.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, D2D1.AlphaMode.Premultiplied);

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

            // Create render texture queue
            {
                var description = new D3D11.Texture2DDescription()
                {
                    Width = FrameWidth,
                    Height = FrameHeight,
                    Format = DXGI.Format.B8G8R8A8_UNorm,

                    ArraySize = 1,
                    MipLevels = 1,
                    BindFlags = D3D11.BindFlags.RenderTarget,
                    SampleDescription = new DXGI.SampleDescription(1, 0),
                    OptionFlags = D3D11.ResourceOptionFlags.None,
                };

                for (int i = 0; i < VideoFrameQueueSize; ++i)
                {
                    _renderQueue.Enqueue(i);

                    var renderTexture = new D3D11.Texture2D(_device3D, description);
                    _renderTextures.Add(renderTexture);

                    using (var surface = renderTexture.QueryInterface<DXGI.Surface>())
                    {
                        var renderTarget = new D2D1.Bitmap1(_context2D, surface);
                        _renderTargets.Add(renderTarget);

                        _context2D.Target = renderTarget;

                        _context2D.BeginDraw();
                        _context2D.DrawBitmap(_backgroundBitmap, new RawRectangleF(0, 0, FrameWidth, FrameHeight), 1, D2D1.BitmapInterpolationMode.NearestNeighbor);
                        _context2D.EndDraw();
                    }
                }
            }

        }

        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public ObservableVideoTrack VideoTrack { get; }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                DisposeAllFields();
            }
        }

        public void SendFrame(TimeSpan elapsedTime, int frameIndex)
        {
            if (_renderQueue.TryDequeue(out long frameId))
            {
                int index = (int) frameId;
                var target = _renderTargets[index];

                // TODO: Draw bouncing ball.
                var y = (float)(elapsedTime.TotalSeconds * 10);
                _context2D.Target = target;

                _context2D.BeginDraw();
                _context2D.DrawBitmap(_backgroundBitmap, new RawRectangleF(0, y, FrameWidth, FrameHeight), 1, D2D1.BitmapInterpolationMode.NearestNeighbor);
                _context2D.EndDraw();

                VideoTrack.SendVideoFrame(frameId, _renderTextures[index].NativePointer, FrameWidth * 4, FrameWidth, FrameHeight, VideoFrameFormat.GpuTextureD3D11);
            }
            else
            {
                Console.WriteLine("WARNING: All D3D11 textures are used by the H264 encoder!");
            }
            
        }

        private void OnLocalVideoFrameEncoded(VideoFrameMessage message)
        {
            // Put the texture back in the queue
            _renderQueue.Enqueue(message.FrameId);
        }
    }
}
