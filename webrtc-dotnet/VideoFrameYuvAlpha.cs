using System;

namespace WonderMediaProductions.WebRtc
{
    public sealed class VideoFrameYuvAlpha : VideoFrame
    {
        // TODO: Convert IntPtr to Memory as soon as this is part of .NET Standard
        public readonly IntPtr DataY;
        public readonly IntPtr DataU;
        public readonly IntPtr DataV;
        public readonly IntPtr DataA;
        public readonly int StrideY;
        public readonly int StrideU;
        public readonly int StrideV;
        public readonly int StrideA;

        public VideoFrameYuvAlpha(IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA, int strideY, int strideU, int strideV, int strideA, int width, int height, long timeStampUs)
            : base(width, height, timeStampUs)
        {
            DataY = dataY;
            DataU = dataU;
            DataV = dataV;
            DataA = dataA;
            StrideY = strideY;
            StrideU = strideU;
            StrideV = strideV;
            StrideA = strideA;
        }
    }
}