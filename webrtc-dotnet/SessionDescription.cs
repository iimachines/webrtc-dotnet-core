using Newtonsoft.Json.Linq;

namespace WonderMediaProductions.WebRtc
{
    public sealed class SessionDescription
    {
        public SessionDescription(string type, string sdp)
        {
            Type = type;
            Sdp = sdp;
        }

        public SessionDescription(JToken json, string keyType = "type", string keySdp = "sdp")
        {
            Type = json.Value<string>(keyType);
            Sdp = json.Value<string>(keySdp);
        }

        public string Type { get; }
        public string Sdp { get; }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(Sdp)}: {Sdp}";
        }
    }
}