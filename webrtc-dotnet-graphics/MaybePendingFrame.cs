using System;
using JetBrains.Annotations;

namespace WonderMediaProductions.WebRtc.GraphicsD3D11
{
    /// <summary>
    /// An frame waiting to be send, or null
    /// </summary>
    public struct MaybePendingFrame : IDisposable
    {
        [CanBeNull]
        internal readonly VideoRenderer Renderer;

        [CanBeNull]
        internal readonly VideoFrame Frame;

        internal readonly long FrameId;

        internal MaybePendingFrame(VideoRenderer renderer, VideoFrame frame, long frameId)
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

        public bool TryGetFrame<T>(out T frame) where T: VideoFrame
        {
            frame = Frame as T;
            return frame != null;
        }

        public void Dispose()
        {
            Renderer?.FinishDequeuedFrame(this);
        }
    }
}