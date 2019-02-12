using System;
using JetBrains.Annotations;

namespace WonderMediaProductions.WebRtc.GraphicsD3D11
{
    public struct MaybeFrame : IDisposable
    {
        [CanBeNull]
        internal readonly VideoRenderer Renderer;

        [CanBeNull]
        internal readonly VideoFrame Frame;

        internal readonly long FrameId;

        internal MaybeFrame(VideoRenderer renderer, VideoFrame frame, long frameId)
        {
            FrameId = frameId;
            Renderer = renderer;
            Frame = frame;
        }

        public bool TryGetFrame(out VideoFrame frame)
        {
            frame = Frame;
            return frame != null;
        }

        public void Dispose()
        {
            Renderer?.Release(this);
        }
    }
}