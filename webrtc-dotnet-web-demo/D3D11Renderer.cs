using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using SharpDX;
using SharpDX.IO;

using D2D1 = SharpDX.Direct2D1;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using WIC = SharpDX.WIC;
using DWrite = SharpDX.DirectWrite;

namespace WonderMediaProductions.WebRtc
{
    public class D3D11Renderer :  Disposable, IRenderer
    {
        private const int NVidiaVendorId = 4318;

        private const int VideoFrameQueueSize = 3;

        private readonly string _backgroundPath;
        private readonly D2D1.PixelFormat _d2dPixelFormat;
        private readonly Guid _wicPixelFormat;
        private readonly D2D1.BitmapProperties1 _d2dBitmapProps;
        private readonly DXGI.Factory2 _dxgiFactory;
        private readonly D3D11.Device _device3D;
        private readonly DXGI.Adapter _adapterDxgi;
        private readonly D3D11.DeviceContext4 _context3D;
        private readonly D3D11.Multithread _threadLock;
        private readonly DXGI.Device4 _deviceDxgi4;
        private readonly DXGI.Factory2 _factoryDgxi2;
        private readonly D2D1.Factory1 _factoryD2D1;
        private readonly DWrite.Factory _factoryDWrite;
        private readonly WIC.ImagingFactory2 _factoryWic;
        private readonly D2D1.Device _device2D;
        private readonly D2D1.DeviceContext _context2D;

        public D3D11Renderer(int frameWidth, int frameHeight, ObservableVideoTrack videoTrack)
        {
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            VideoTrack = videoTrack;

            _backgroundPath = "background-small.jpg";

            // specify a pixel format that is supported by both D2D and WIC
            _d2dPixelFormat = new D2D1.PixelFormat(DXGI.Format.R8G8B8A8_UNorm, D2D1.AlphaMode.Premultiplied);

            // if in D2D was specified an R-G-B-A format - use the same for wic
            _wicPixelFormat = WIC.PixelFormat.Format32bppPRGBA;

            // create the d2d bitmap description using default flags (from SharpDX samples) and 96 DPI
            _d2dBitmapProps = new D2D1.BitmapProperties1(_d2dPixelFormat, 96, 96, D2D1.BitmapOptions.Target | D2D1.BitmapOptions.CannotDraw);

            _dxgiFactory = new DXGI.Factory2(debug: true);

            D3D11.DeviceCreationFlags creationFlags = D3D11.DeviceCreationFlags.BgraSupport | D3D11.DeviceCreationFlags.Debug;

            // We require an NVidia adapter
            using (var adapters = _dxgiFactory.Adapters.ToDisposableList())
            {
                // Try an NVidia adapter first, but we need a device that supports the required feature levels.
                var nvAdapter = adapters.Single(a => a.Description.VendorId == NVidiaVendorId);
                var requiredFeatureLevels3D = new [] {D3D.FeatureLevel.Level_11_1};
                _device3D = new D3D11.Device(nvAdapter, creationFlags, requiredFeatureLevels3D);
                _adapterDxgi = new DXGI.Adapter(nvAdapter.NativePointer);
                _context3D = _device3D.ImmediateContext.QueryInterface<D3D11.DeviceContext4>();

                // We need to access D3D11 on multiple threads, so enable multi-threading
                _threadLock = _device3D.ImmediateContext.QueryInterface<D3D11.Multithread>();
                _threadLock.SetMultithreadProtected(true);

                _deviceDxgi4 = _device3D.QueryInterface<DXGI.Device4>();
                // var dxgiAdapter = dxgiDevice4.Adapter;
                _factoryDgxi2 = _adapterDxgi.GetParent<DXGI.Factory2>();
                _factoryD2D1 = new D2D1.Factory1(D2D1.FactoryType.SingleThreaded, D2D1.DebugLevel.Warning);
                _factoryDWrite = new DWrite.Factory(DWrite.FactoryType.Shared);
                _factoryWic = new WIC.ImagingFactory2();

                using (var dxgiDevice = _device3D.QueryInterface<DXGI.Device>())
                {
                    _device2D = new D2D1.Device(_factoryD2D1, dxgiDevice);
                    _context2D = new D2D1.DeviceContext(_device2D, D2D1.DeviceContextOptions.None);
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
        }
    }
}
