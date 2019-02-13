using System;
using System.Runtime.InteropServices;
using SharpDX.Mathematics.Interop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Shapes;
using PixelColor = SixLabors.ImageSharp.PixelFormats.Bgra32;

namespace WonderMediaProductions.WebRtc
{
    /// <summary>
    /// Renders a bouncing ball using ImageSharp
    /// </summary>
    public class ImageSharpRenderer : Disposable, IRenderer
    {
        private const int FrameCount = 60;

        private readonly DisposableList<Image<PixelColor>> _videoFrames = new DisposableList<Image<Bgra32>>();

        public ImageSharpRenderer(int frameWidth, int frameHeight, VideoTrack videoTrack)
        {
            VideoTrack = videoTrack;

            using (var background = Image.Load<PixelColor>("background-small.jpg"))
            {
                background.Mutate(ctx => ctx.Resize(frameWidth, frameHeight));

                // Pre-created bouncing ball frames.
                // ImageSharp is not that fast yet, and our goal is to benchmark webrtc and NvEnc, not ImageSharp.
                var ballRadius = background.Width / 20f;
                var ballPath = new EllipsePolygon(0, 0, ballRadius);
                var ballColor = new PixelColor(255, 255, 128);

                for (int i = 0; i < FrameCount; ++i)
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var image = background.Clone();

                    var a = Math.PI * i / FrameCount;
                    var h = image.Height - ballRadius;
                    var y = image.Height - (float) (Math.Abs(Math.Sin(a) * h));

                    image.Mutate(ctx => ctx
                        .Fill(GraphicsOptions.Default, ballColor, ballPath.Translate(image.Width / 2f, y)));

                    _videoFrames.Add(image);
                }
            }
        }

        public VideoTrack VideoTrack { get; }
        public RawVector2? BallPosition { get; set; }

        public bool SendFrame(TimeSpan elapsedTime, int frameIndex)
        {
            var imageFrameIndex = frameIndex % FrameCount;
            var imageFrame = _videoFrames[imageFrameIndex].Frames[0];
            var pixels = MemoryMarshal.Cast<PixelColor, uint>(imageFrame.GetPixelSpan());

            var format = PeerConnection.SupportsHardwareTextureEncoding
                ? VideoFrameFormat.CpuTexture
                : VideoFrameFormat.Bgra32;

            VideoTrack.SendVideoFrame(MemoryMarshal.GetReference(pixels),
                imageFrame.Width * 4,
                imageFrame.Width,
                imageFrame.Height,
                format);

            return true;
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                DisposeAllFields();
            }
        }
    }
}