using System;
using SharpDX.Mathematics.Interop;

namespace WonderMediaProductions.WebRtc
{

    public interface IRenderer : IDisposable
    {
        int VideoFrameWidth { get; }
        int VideoFrameHeight { get; }

        ObservableVideoTrack VideoTrack { get; }

        RawVector2? BallPosition { get; set; }

        bool SendFrame(TimeSpan elapsedTime, int frameIndex);
    }
}