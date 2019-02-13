using System;

namespace WonderMediaProductions.WebRtc
{
    public sealed class VideoFrameTexture : VideoFrame
    {
        public readonly IntPtr TexturePtr;

        public VideoFrameTexture(IntPtr texturePtr, int width, int height, long timeStampUs)
            : base(width, height, timeStampUs)
        {
            TexturePtr = texturePtr;
        }
    }
}