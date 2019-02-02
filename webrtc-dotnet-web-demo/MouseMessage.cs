using Newtonsoft.Json.Linq;
using SharpDX.Mathematics.Interop;

namespace WonderMediaProductions.WebRtc
{
    public sealed class MouseMessage
    {
        public readonly MouseEventKind Kind;
        public readonly RawVector2 Pos;

        public MouseMessage(MouseEventKind kind, RawVector2 pos)
        {
            Kind = kind;
            Pos = pos;
        }

        public MouseMessage(JToken json, string keyKind = "kind", string keyX = "x", string keyY = "y")
        {
            Kind = (MouseEventKind) json.Value<int>(keyKind);
            Pos.X = json.Value<float>(keyX);
            Pos.Y = json.Value<float>(keyY);
        }
    }
}