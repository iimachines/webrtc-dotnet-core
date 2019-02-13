namespace WonderMediaProductions.WebRtc
{
    /// <summary>
    /// https://w3c.github.io/webrtc-pc/#dom-rtcpeerconnectionstate
    /// </summary>
    public enum PeerConnectionState
    {
        New,
        Connecting,
        Connected,
        Disconnected,
        Failed,
        Closed,
    };
}