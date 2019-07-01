using System;
using SharpDX.Mathematics.Interop;

namespace WonderMediaProductions.WebRtc
{
    public interface IRenderer : IDisposable
    {
        RawVector2? MousePosition { get; set; }

        bool SendFrame(TimeSpan elapsedTime);
    }
}