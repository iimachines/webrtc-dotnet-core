using System;
using SharpDX;
using SharpDX.IO;
using SharpDX.Mathematics.Interop;
using WonderMediaProductions.WebRtc.GraphicsD3D11;
using D2D1 = SharpDX.Direct2D1;
using WIC = SharpDX.WIC;

namespace WonderMediaProductions.WebRtc
{
    /// <summary>
    /// Renders a bouncing ball using D3D11 textures
    /// </summary>
    public sealed class D3D11Renderer : VideoRenderer, IRenderer
    {
        private readonly D2D1.DeviceContext _context2D;
        private readonly D2D1.Bitmap1 _backgroundBitmap;
        private readonly D2D1.Ellipse _ballEllipse;
        private readonly D2D1.Brush _ballBrush;

        public D3D11Renderer(ObservableVideoTrack videoTrack, RendererOptions options)
            : base(videoTrack, options)
        {
            // _factoryDWrite = new DWrite.Factory(DWrite.FactoryType.Shared);

            var device2D = new D2D1.Device(DeviceDXGI, new D2D1.CreationProperties
            {
                DebugLevel = D2D1.DebugLevel.Warning,
                ThreadingMode = D2D1.ThreadingMode.MultiThreaded,
                Options = D2D1.DeviceContextOptions.None
            });

            _context2D = new D2D1.DeviceContext(device2D, D2D1.DeviceContextOptions.None);

            // Load the background image
            using (var factoryWic = new WIC.ImagingFactory2())
            using (var decoder = new WIC.JpegBitmapDecoder(factoryWic))
            using (var inputStream = new WIC.WICStream(factoryWic, "background-small.jpg", NativeFileAccess.Read))
            using (var formatConverter = new WIC.FormatConverter(factoryWic))
            using (var bitmapScaler = new WIC.BitmapScaler(factoryWic))
            {
                decoder.Initialize(inputStream, WIC.DecodeOptions.CacheOnLoad);
                formatConverter.Initialize(decoder.GetFrame(0), WIC.PixelFormat.Format32bppPBGRA);
                bitmapScaler.Initialize(formatConverter, VideoFrameWidth, VideoFrameHeight,
                    WIC.BitmapInterpolationMode.Fant);
                _backgroundBitmap = D2D1.Bitmap1.FromWicBitmap(_context2D, bitmapScaler);
            }

            // Create render target
            _ballEllipse = new D2D1.Ellipse { RadiusX = VideoFrameWidth / 20f, RadiusY = VideoFrameWidth / 20f };

            _ballBrush = new D2D1.SolidColorBrush(_context2D, new RawColor4(1f, 1f, 0f, 1f));
        }

        public new ObservableVideoTrack VideoTrack => (ObservableVideoTrack)base.VideoTrack;

        public RawVector2? BallPosition { get; set; }

        protected override VideoFrameBuffer OnCreateFrame() => new FrameD3D11(this, _context2D);

        public bool SendFrame(TimeSpan elapsedTime, int frameIndex)
        {
            const int BallCount = 10;

            using (var df = TakeNextFrameForSending())
            {
                if (!df.TryGetFrame(out FrameD3D11 frame))
                    return false;

                _context2D.Target = frame.Bitmap;
                _context2D.BeginDraw();

                // TODO: Draw bouncing ball.
                _context2D.Transform = Matrix3x2.Identity;
                _context2D.DrawBitmap(_backgroundBitmap, new RawRectangleF(0, 0, VideoFrameWidth, VideoFrameHeight),
                    1, D2D1.BitmapInterpolationMode.NearestNeighbor);

                // Draw many balls to simulate high motion
                for (int i = 0; i < BallCount; ++i)
                {
                    var a = 2 * Math.PI * elapsedTime.TotalSeconds + i * Math.PI / BallCount;
                    var h = VideoFrameHeight - _ballEllipse.RadiusY;
                    var y = (float) (VideoFrameHeight - Math.Abs(Math.Sin(a) * h));
                    var pos = new RawVector2(i * (VideoFrameWidth - _ballEllipse.RadiusX*2f) / (BallCount-1) + _ballEllipse.RadiusX, y);

                    _context2D.Transform = BallPosition.HasValue
                        ? Matrix3x2.Translation(BallPosition.Value * new Vector2(VideoFrameWidth, VideoFrameHeight))
                        : Matrix3x2.Translation(pos);

                    _context2D.FillEllipse(_ballEllipse, _ballBrush);
                }

                _context2D.EndDraw();
                _context2D.Target = null;

                return true;
            }
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _context2D?.Dispose();
                _backgroundBitmap?.Dispose();
                _ballBrush?.Dispose();
            }

            base.OnDispose(isDisposing);
        }
    }
}
