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

        public string Candidate { get; }
        public int SdpMlineIndex { get; }
        public string SdpMid { get; }

        public override string ToString()
        {
            return $"{nameof(Candidate)}: {Candidate}, {nameof(SdpMlineIndex)}: {SdpMlineIndex}, {nameof(SdpMid)}: {SdpMid}";
        }
    }
}