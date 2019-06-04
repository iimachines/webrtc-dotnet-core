using Newtonsoft.Json;

namespace WonderMediaProductions.WebRtc
{
    public sealed class SessionDescription
    {
	    public SessionDescription(string type, string sdp)
        {
            Type = type;
            Sdp = sdp;
        }

	    [JsonProperty("type")]
	    public readonly string Type;

	    [JsonProperty("sdp")]
	    public readonly string Sdp;

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(Sdp)}: {Sdp}";
        }
    }
}
