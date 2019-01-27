namespace webrtc_dotnet_standard
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