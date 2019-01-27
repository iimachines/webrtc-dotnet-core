namespace webrtc_dotnet_standard
{
    /// <summary>
    /// https://w3c.github.io/webrtc-pc/#dom-rtciceconnectionstate
    /// </summary>
    public enum IceConnectionState
    {
        New,
        Checking,
        Connected,
        Completed,
        Failed,
        Disconnected,
        Closed,
        Max,
    };
}