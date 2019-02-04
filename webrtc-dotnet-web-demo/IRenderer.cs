using System;
using SharpDX.Mathematics.Interop;

namespace WonderMediaProductions.WebRtc
{

    public interface IRenderer : IDisposable
    {
        int FrameWidth { get; }
        int FrameHeight { get; }

        ObservableVideoTrack VideoTrack { get; }

        RawVector2? BallPosition { get; set; }

        bool SendFrame(TimeSpan elapsedTime, int frameIndex);
    }
}