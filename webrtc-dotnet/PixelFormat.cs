namespace webrtc_dotnet_standard
{
    public enum PixelFormat
    {
        Rgba32, // 8-bit R G B A in memory 
        Bgra32, // 8-bit B G R A in memory 
        Argb32, // 8-bit A R G B in memory 
        Abgr32, // 8-bit A B G R in memory 
        Texture // A native texture, not supported yet
    };
}