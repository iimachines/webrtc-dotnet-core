using System;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace WonderMediaProductions.WebRtc.GraphicsD3D11
{
    public class VideoFrame : Disposable
    {
        public D3D11.Texture2D Texture { get; }

        public VideoFrame(D3D11.Device device3D, D3D11.Texture2DDescription textureDescription)
        {
            Texture = new D3D11.Texture2D(device3D, textureDescription);
        }

        public VideoFrame(D3D11.Device device3D, int width, int height) 
            : this(device3D, new D3D11.Texture2DDescription()
            {
                Width = width,
                Height = height,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                BindFlags = D3D11.BindFlags.RenderTarget,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                OptionFlags = D3D11.ResourceOptionFlags.None,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                Usage = D3D11.ResourceUsage.Default
            })
        {
        }

        public VideoFrame(VideoRenderer renderer)
            : this(renderer.Device3D, renderer.VideoFrameWidth, renderer.VideoFrameHeight)
        {

        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                Texture?.Dispose();
            }
        }

        public virtual void Send(VideoTrack videoTrack)
        {
            var description = Texture.Description;

            videoTrack.SendVideoFrame(Texture.NativePointer,
                0, description.Width, description.Height, 
                VideoFrameFormat.GpuTextureD3D11);
        }
    }
}