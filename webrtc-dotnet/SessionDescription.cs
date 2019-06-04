using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WonderMediaProductions.WebRtc
{
    public sealed class SessionDescription
    {
	    private SessionDescription()
	    {
	    }

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

		[JsonProperty("type")]
        public string Type { get; }

        [JsonProperty("sdp")]
        public string Sdp { get; }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(Sdp)}: {Sdp}";
        }
    }
}
