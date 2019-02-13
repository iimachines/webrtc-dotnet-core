using System;
using JetBrains.Annotations;

namespace WonderMediaProductions.WebRtc.GraphicsD3D11
{
    /// <summary>
    /// An frame waiting to be send, or null
    /// </summary>
    public struct MaybeSendableFrame : IDisposable
    {
        [CanBeNull]
        internal readonly VideoRenderer Renderer;

        [CanBeNull]
        internal readonly VideoFrameBuffer Frame;

        internal MaybeSendableFrame(VideoRenderer renderer, VideoFrameBuffer frame)
        {
            Renderer = renderer;
            Frame = frame;
        }

        public bool TryGetFrame(out VideoFrameBuffer frame)
        {
            frame = Frame;
            return frame != null;
        }

        public bool TryGetFrame<T>(out T frame) where T: VideoFrameBuffer
        {
            frame = Frame as T;
            return frame != null;
        }

        public void Dispose()
        {
            Renderer?.TransmitSendableFrame(this);
        }
    }
}