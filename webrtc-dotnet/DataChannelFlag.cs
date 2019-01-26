using System;

namespace webrtc_dotnet_standard
{
    [Flags]
    public enum DataChannelFlag
    {
        None = 0,
        Reliable = 1, // Only works for SRTP it seems
        Ordered = 2
    }
}