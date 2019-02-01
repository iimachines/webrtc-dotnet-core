using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using SharpDX.Mathematics.Interop;
using DXGI = SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;

namespace WonderMediaProductions.WebRtc
{
    public class D3D11CudaTest : Disposable, IRenderer
    {
        private const int NVidiaVendorId = 4318;

        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        private readonly DXGI.Factory2 _factoryDgxi;
        private readonly D3D11.Device _device3D;
        private readonly D3D11.DeviceContext _context3D;
        private readonly D3D11.Texture2D _frameTexture;

        private readonly ConcurrentQueue<long> _renderQueue = new ConcurrentQueue<long>();

        public D3D11CudaTest(int frameWidth, int frameHeight, ObservableVideoTrack videoTrack)
        {
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            VideoTrack = videoTrack;

            _subscriptions.Add(videoTrack.LocalVideoFrameEncodedStream.Subscribe(OnLocalVideoFrameEncoded));

            _factoryDgxi = new DXGI.Factory2(debug: true);

            // We require an NVidia adapter
            using (var adapters = _factoryDgxi.Adapters.ToDisposableList())
            {
                var nvAdapter = adapters.First(a => a.Description.VendorId == NVidiaVendorId);
                var requiredFeatureLevels3D = new[] { SharpDX.Direct3D.FeatureLevel.Level_11_1 };
                D3D11.DeviceCreationFlags creationFlags = D3D11.DeviceCreationFlags.Debug;
                _device3D = new D3D11.Device(nvAdapter, creationFlags, requiredFeatureLevels3D);
                _context3D = new D3D11.DeviceContext(_device3D.ImmediateContext.NativePointer);
            }

            var textureDescription = new D3D11.Texture2DDescription()
            {
                Width = FrameWidth,
                Height = FrameHeight,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                BindFlags = D3D11.BindFlags.RenderTarget,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                OptionFlags = D3D11.ResourceOptionFlags.None,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                Usage = D3D11.ResourceUsage.Default
            };

            _frameTexture = new D3D11.Texture2D(_device3D, textureDescription);

            using (var view = new D3D11.RenderTargetView(_device3D, _frameTexture))
            {
                _context3D.ClearRenderTargetView(view, new RawColor4(1.0f, 0.5f, 0.25f, 1.0f));
            }

            _renderQueue.Enqueue(0);
        }

        public int FrameWidth { get; }
        public int FrameHeight { get; }

        public ObservableVideoTrack VideoTrack { get; }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                DisposeAllFields();
            }
        }

        public void SendFrame(TimeSpan elapsedTime, int frameIndex)
        {
            if (_renderQueue.TryDequeue(out long frameId))
            {
                VideoTrack.SendVideoFrame(frameId, _frameTexture.NativePointer, FrameWidth * 4, FrameWidth, FrameHeight, VideoFrameFormat.GpuTextureD3D11);
            }
            else
            {
                Console.WriteLine("WARNING: All D3D11 textures are used by the H264 encoder!");
            }

        }

        private void OnLocalVideoFrameEncoded(VideoFrameMessage message)
        {
            // Put the texture back in the queue
            _renderQueue.Enqueue(message.FrameId);
        }
    }
}