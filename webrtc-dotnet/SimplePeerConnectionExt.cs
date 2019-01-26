using System;

namespace webrtc_dotnet_standard
{
    public static class SimplePeerConnectionExt
    {
        public static string ToPcId(this IntPtr pc)
        {
            return $"PC#{pc.ToInt64():X10}";
        }
    }
}