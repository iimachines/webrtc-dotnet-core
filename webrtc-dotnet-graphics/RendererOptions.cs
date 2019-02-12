using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;

namespace WonderMediaProductions.WebRtc.GraphicsD3D11
{
    public class RendererOptions
    {
        public int VideoFrameWidth = 1920;
        public int VideoFrameHeight = 1080;

        public int VideoFrameQueueSize = 3;

        public int AdapterVendorId = GpuVendorId.NVidia;

        public D3D.FeatureLevel[] FeatureLevels = {D3D.FeatureLevel.Level_11_1};
        public D3D11.DeviceCreationFlags CreationFlags = D3D11.DeviceCreationFlags.BgraSupport;

        public RendererOptions()
        {
#if DEBUG
            CreationFlags |= D3D11.DeviceCreationFlags.Debug;
#endif
        }
    }
}