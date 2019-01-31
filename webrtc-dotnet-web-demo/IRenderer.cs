using System;

namespace WonderMediaProductions.WebRtc
{
    public interface IRenderer : IDisposable
    {
        int FrameWidth { get; }
        int FrameHeight { get; }

        ObservableVideoTrack VideoTrack { get; }

        void SendFrame(TimeSpan elapsedTime, int frameIndex);
    }
}