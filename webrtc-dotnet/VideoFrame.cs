using System;

namespace WonderMediaProductions.WebRtc
{
    public abstract class VideoFrame
    {
        public const long TicksPerUs = TimeSpan.TicksPerMillisecond / 1000;

        public readonly int Width;

        public readonly int Height;

        /// <summary>
        /// TimeStamp since this system started
        /// TODO: We would like to have NTP absolute timestamps, but the webrtc code says this is deprecated?
        /// </summary>
        /// <remarks>
        /// WARNING: TimeSpan is not very precise, it always converts to milliseconds.
        /// </remarks>
        public readonly TimeSpan TimeStamp;

        protected VideoFrame(int width, int height, long timeStampUs)
        {
            Width = width;
            Height = height;
            TimeStamp = TimeSpan.FromTicks(timeStampUs * TicksPerUs);
        }

        internal static VideoFrame FromNative(
            IntPtr texture,
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            int width, int height, long timeStampUs)
        {
            if (texture == IntPtr.Zero)
                return new VideoFrameYuvAlpha(
                    dataY, dataU, dataV, dataA,
                    strideY, strideU, strideV, strideA,
                    width, height, timeStampUs);

            return new VideoFrameTexture(texture, width, height, timeStampUs);
        }
    }
}