using System;

namespace WonderMediaProductions.WebRtc
{
    public sealed class VideoFrameYuvAlpha
    {
        public const long TicksPerUs = TimeSpan.TicksPerMillisecond / 1000;

        public readonly IntPtr DataY;
        public readonly IntPtr DataU;
        public readonly IntPtr DataV;
        public readonly IntPtr DataA;
        public readonly int StrideY;
        public readonly int StrideU;
        public readonly int StrideV;
        public readonly int StrideA;
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

        public VideoFrameYuvAlpha(IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA, int strideY, int strideU, int strideV, int strideA, int width, int height, long timeStampUs)
        {
            DataY = dataY;
            DataU = dataU;
            DataV = dataV;
            DataA = dataA;
            StrideY = strideY;
            StrideU = strideU;
            StrideV = strideV;
            StrideA = strideA;
            Width = width;
            Height = height;
            TimeStamp = TimeSpan.FromTicks(timeStampUs * TicksPerUs);
        }
    }
}