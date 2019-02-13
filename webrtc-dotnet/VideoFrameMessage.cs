using System;

namespace WonderMediaProductions.WebRtc
{
    public struct VideoFrameMessage
    {
        public readonly int TrackId;
        public readonly IntPtr RgbaPixels;

        public VideoFrameMessage(int trackId, IntPtr rgbaPixels)
        {
            TrackId = trackId;
            RgbaPixels = rgbaPixels;
        }
    }
}