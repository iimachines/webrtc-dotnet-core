using System;
using SharpDX.Mathematics.Interop;

namespace WonderMediaProductions.WebRtc
{

    public interface IRenderer : IDisposable
    {
        RawVector2? BallPosition { get; set; }

        bool SendFrame(TimeSpan elapsedTime, int frameIndex);
    }
}