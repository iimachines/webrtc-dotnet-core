namespace webrtc_dotnet_standard
{
    /// <summary>
    /// See https://w3c.github.io/webrtc-pc/#dom-rtcsignalingstate
    /// </summary>
    public enum SignalingState
    {
        Stable,
        HaveLocalOffer,
        HaveLocalPrAnswer,
        HaveRemoteOffer,
        HaveRemotePrAnswer,
        Closed,
    };
}