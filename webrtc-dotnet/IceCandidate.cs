using Newtonsoft.Json.Linq;

namespace webrtc_dotnet_standard
{
    public sealed class IceCandidate
    {
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

        public string Candidate { get; }
        public int SdpMlineIndex { get; }
        public string SdpMid { get; }

        public override string ToString()
        {
            return $"{nameof(Candidate)}: {Candidate}, {nameof(SdpMlineIndex)}: {SdpMlineIndex}, {nameof(SdpMid)}: {SdpMid}";
        }
    }
}