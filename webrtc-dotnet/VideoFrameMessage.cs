using System;

namespace WonderMediaProductions.WebRtc
{
    public struct VideoFrameMessage
    {
        public readonly int TrackId;
        public readonly IntPtr RgbaPixels;
        public readonly bool IsEncoded;

        public VideoFrameMessage(int trackId, IntPtr rgbaPixels, bool isEncoded)
        {
            TrackId = trackId;
            RgbaPixels = rgbaPixels;
            IsEncoded = isEncoded;
        }
    }
}