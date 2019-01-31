using System;

namespace WonderMediaProductions.WebRtc
{
    public sealed class VideoFrameMessage
    {
        public readonly int TrackId;
        public readonly long FrameId;
        public readonly IntPtr RgbaPixels;

        public VideoFrameMessage(int trackId, long frameId, IntPtr rgbaPixels)
        {
            TrackId = trackId;
            FrameId = frameId;
            RgbaPixels = rgbaPixels;
        }
    }
}