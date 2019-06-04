using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WonderMediaProductions.WebRtc
{
    public sealed class IceCandidate
    {
	    private IceCandidate()
	    {
	    }

		public IceCandidate(string candidate, int sdpMlineIndex, string sdpMid)
        {
            Candidate = candidate;
            SdpMlineIndex = sdpMlineIndex;
            SdpMid = sdpMid;
        }

        public IceCandidate(JToken json, string keyCandidate = "candidate", string keySdpMlineIndex = "sdpMlineIndex", string keySdpMid = "sdpMid")
        {
            Candidate = json.Value<string>(keyCandidate);
            SdpMlineIndex = json.Value<int>(keySdpMlineIndex);
            SdpMid = json.Value<string>(keySdpMid);
        }

		[JsonProperty("candidate")]
        public string Candidate { get; }

        [JsonProperty("sdpMlineIndex")]
        public int SdpMlineIndex { get; }

        [JsonProperty("sdpMid")]
		public string SdpMid { get; }

        public override string ToString()
        {
            return $"{nameof(Candidate)}: {Candidate}, {nameof(SdpMlineIndex)}: {SdpMlineIndex}, {nameof(SdpMid)}: {SdpMid}";
        }
    }
}
