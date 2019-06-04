using Newtonsoft.Json;

namespace WonderMediaProductions.WebRtc
{
    public sealed class IceCandidate
    {
		public IceCandidate(string candidate, int sdpMLineIndex, string sdpMid)
        {
            Candidate = candidate;
            SdpMLineIndex = sdpMLineIndex;
            SdpMid = sdpMid;
        }

		[JsonProperty("candidate")]
		public readonly string Candidate;

		[JsonProperty("sdpMLineIndex")]
		public readonly int SdpMLineIndex;

		[JsonProperty("sdpMid")]
		public readonly string SdpMid;

        public override string ToString()
        {
            return $"{nameof(Candidate)}: {Candidate}, {nameof(SdpMLineIndex)}: {SdpMLineIndex}, {nameof(SdpMid)}: {SdpMid}";
        }
    }
}
