using D2D1 = SharpDX.Direct2D1;

namespace WonderMediaProductions.WebRtc
{
    public sealed class BitmapFrameD2D1 : GraphicsD3D11.VideoFrameBuffer
    {
        public readonly D2D1.Bitmap1 Bitmap;

        public BitmapFrameD2D1(BouncingBallRenderer renderer, D2D1.DeviceContext context2D)
            : base(renderer)
        {
            using (var surface = Texture.QueryInterface<SharpDX.DXGI.Surface>())
            {
                Bitmap = new D2D1.Bitmap1(context2D, surface);
            }
        }
        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                Bitmap?.Dispose();
            }

            base.OnDispose(isDisposing);
        }
    }
}