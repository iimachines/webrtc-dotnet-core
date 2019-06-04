using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.Mathematics.Interop;

namespace WonderMediaProductions.WebRtc
{
    public sealed class MouseMessage
    {
		[JsonProperty("kind")]
        public readonly MouseEventKind Kind;

        [JsonProperty("x")]
		public readonly float X;

		[JsonProperty("y")]
		public readonly float Y;

        public MouseMessage(MouseEventKind kind, float x, float y)
        {
            Kind = kind;
            X = x;
            Y = y;
        }

		[JsonIgnore]
        public RawVector2 Pos => new RawVector2(X, Y);
    }
}
