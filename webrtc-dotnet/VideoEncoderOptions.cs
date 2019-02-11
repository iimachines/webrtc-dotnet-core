namespace WonderMediaProductions.WebRtc
{
    public sealed class VideoEncoderOptions
    {
        public string Label = "video";

        public int MaxFramesPerSecond = 30;

        public int MinBitsPerSecond = OptimalBitsPerSecond(640, 480, 30, VideoMotion.Low);

        public int MaxBitsPerSecond = OptimalBitsPerSecond(640, 480, 30, VideoMotion.High);

        public static VideoEncoderOptions OptimizedFor(int width, int height, int maxFramesPerSecond)
        {
            return new VideoEncoderOptions
            {
                MaxFramesPerSecond = maxFramesPerSecond,
                MinBitsPerSecond = OptimalBitsPerSecond(width, height, maxFramesPerSecond, VideoMotion.Low),
                MaxBitsPerSecond = OptimalBitsPerSecond(width, height, maxFramesPerSecond, VideoMotion.High)
            };
        }

        /// <summary>
        /// http://blog.sporv.com/restoration-tips-kush-gauge/
        /// </summary>
        public static int OptimalBitsPerSecond(int width, int height, int framesPerSecond, VideoMotion motionAmount)
        {
            long area = width * height;
            return (int)(area * framesPerSecond * (int)motionAmount * 7 / 100);
        }
    }
}