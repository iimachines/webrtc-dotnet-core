using System;
using SharpDX;
using SharpDX.IO;
using SharpDX.Mathematics.Interop;
using WonderMediaProductions.WebRtc.GraphicsD3D11;
using D2D1 = SharpDX.Direct2D1;
using WIC = SharpDX.WIC;

namespace WonderMediaProductions.WebRtc
{
    public class TimeRulerOptions
    {
        public TimeSpan Duration = TimeSpan.FromSeconds(1);

        public int MarkerCount = 60;
        public int MarkerWidth = 7;
        public int MarkerHeight = 50;
    }

    public sealed class BoundingBallOptions : RendererOptions
    {
        public TimeRulerOptions TimeRulerOptions;
    }

    /// <summary>
    /// Renders a bouncing ball using a D2D1 bitmap.
    /// </summary>
    public sealed class BouncingBallRenderer : VideoRenderer, IRenderer
    {
        public int BallCount { get; }

        private readonly D2D1.DeviceContext _context2D;
        private readonly D2D1.Bitmap1 _backgroundBitmap;
        private readonly D2D1.Ellipse _ballEllipse;
        private readonly D2D1.Brush _ballBrush;
        private readonly D2D1.Brush _timeMarkerBrush1;
        private readonly D2D1.Brush _timeMarkerBrush2;
        private readonly D2D1.Brush _currentTimeBrush;
        private readonly D2D1.Brush _timeRulerBrush;
        
        private readonly TimeRulerOptions _rulerOptions;

        public BouncingBallRenderer(ObservableVideoTrack videoTrack, int ballCount, BoundingBallOptions options)
            : base(videoTrack, options)
        {
            _rulerOptions = options.TimeRulerOptions;

            BallCount = ballCount;

            // _factoryDWrite = new DWrite.Factory(DWrite.FactoryType.Shared);

            var device2D = new D2D1.Device(DeviceDXGI, new D2D1.CreationProperties
            {
                DebugLevel = D2D1.DebugLevel.Warning,
                ThreadingMode = D2D1.ThreadingMode.MultiThreaded,
                Options = D2D1.DeviceContextOptions.None,
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
            _timeMarkerBrush1 = new D2D1.SolidColorBrush(_context2D, new RawColor4(0.0f, 0.0f, 0.5f, 1f));
            _timeMarkerBrush2 = new D2D1.SolidColorBrush(_context2D, new RawColor4(0.0f, 0.75f, 0.0f, 1f));
            _currentTimeBrush = new D2D1.SolidColorBrush(_context2D, new RawColor4(1.0f, 0.0f, 0.0f, 1f));
            _timeRulerBrush = new D2D1.SolidColorBrush(_context2D, new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));
        }

        public new ObservableVideoTrack VideoTrack => (ObservableVideoTrack)base.VideoTrack;

        public RawVector2? MousePosition { get; set; }

        protected override VideoFrameBuffer OnCreateFrame() => new BitmapFrameD2D1(this, _context2D);

        public bool SendFrame(TimeSpan elapsedTime)
        {
            using (var df = TakeNextFrameForSending())
            {
                if (!df.TryGetFrame(out BitmapFrameD2D1 frame))
                    return false;

                _context2D.Target = frame.Bitmap;
                _context2D.BeginDraw();

                _context2D.Transform = Matrix3x2.Identity;
                _context2D.DrawBitmap(_backgroundBitmap, new RawRectangleF(0, 0, VideoFrameWidth, VideoFrameHeight),
                    1, D2D1.BitmapInterpolationMode.NearestNeighbor);

                // Draw many balls to simulate high motion
                for (int i = 0; i < BallCount; ++i)
                {
                    var a = 2 * Math.PI * elapsedTime.TotalSeconds + i * Math.PI / BallCount;
                    var h = VideoFrameHeight - _ballEllipse.RadiusY;
                    var s = (float)VideoFrameWidth / (BallCount + 1);
                    var y = (float)(VideoFrameHeight - Math.Abs(Math.Sin(a) * h));
                    var pos = new RawVector2((i + 1) * s, y); // - _ballEllipse.RadiusX, y);

                    _context2D.Transform = MousePosition.HasValue
                        ? Matrix3x2.Translation(MousePosition.Value * new Vector2(VideoFrameWidth, VideoFrameHeight))
                        : Matrix3x2.Translation(pos);

                    _context2D.FillEllipse(_ballEllipse, _ballBrush);
                }

                if (_rulerOptions != null)
                {
                    _context2D.Transform = Matrix3x2.Identity;

                    // Draw the time markers and current time to measure latency
                    int markerCount = _rulerOptions.MarkerCount;
                    int markerWidth = _rulerOptions.MarkerWidth;
                    int markerHeight = _rulerOptions.MarkerHeight;
                    int markerOffset = markerWidth / 2;

                    var y = VideoFrameHeight - markerHeight;
                    _context2D.FillRectangle(new RectangleF(0, y, VideoFrameWidth, markerHeight), _timeRulerBrush);

                    for (int i = 0; i < markerCount; ++i)
                    {
                        var x = markerOffset + i * (VideoFrameWidth - markerWidth) / markerCount;
                        var b = i % 10 == 0 ? _timeMarkerBrush2 : _timeMarkerBrush1;

                        var r = new RectangleF(x - markerOffset, y, markerWidth, markerHeight);
                        _context2D.FillRectangle(r, b);
                    }

                    // Draw current time
                    {
                        var duration = _rulerOptions.Duration.TotalMilliseconds;
                        var t = elapsedTime.TotalMilliseconds % duration;
                        var i = (float)(t * markerCount / duration);

                        var x = markerOffset + i * (VideoFrameWidth - markerWidth) / markerCount;
                        var r = new RectangleF(x - markerOffset, y, markerWidth, markerHeight);
                        _context2D.FillRectangle(r, _currentTimeBrush);
                    }
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
                _timeMarkerBrush1?.Dispose();
                _timeMarkerBrush2?.Dispose();
                _currentTimeBrush?.Dispose();
                _timeRulerBrush?.Dispose();
            }

            base.OnDispose(isDisposing);
        }
    }
}
