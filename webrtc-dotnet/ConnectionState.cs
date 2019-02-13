namespace WonderMediaProductions.WebRtc
{
    /// <summary>
    /// https://w3c.github.io/webrtc-pc/#dom-rtcpeerconnectionstate
    /// </summary>
    public enum ConnectionState
    {
        New,
        Connecting,
        Connected,
        Disconnected,
        Failed,
        Closed,
    };
}