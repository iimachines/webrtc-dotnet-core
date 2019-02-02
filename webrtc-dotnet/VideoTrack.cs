using System;

namespace WonderMediaProductions.WebRtc
{
    public class VideoTrack : Disposable
    {
        public int TrackId { get; }

        public PeerConnection PeerConnection { get; }

        // TODO: Must be a rational number
        public int FrameRate { get; }

        public event VideoFrameEncodedDelegate LocalVideoFrameEncoded;

        public VideoTrack(PeerConnection peerConnection, Action<VideoEncoderOptions> configure)
        {
            var options = configure.Options();
            PeerConnection = peerConnection;
            FrameRate = options.MaxFramesPerSecond;
            TrackId = peerConnection.RegisterVideoTrack(options);
            PeerConnection.LocalVideoFrameEncoded += OnLocalVideoFrameEncoded;
        }

        public unsafe void SendVideoFrame(long frameId, in uint rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat)
        {
            fixed (uint* ptr = &rgbaPixels)
            {
                PeerConnection.SendVideoFrame(TrackId, frameId, new IntPtr(ptr), stride, width, height, videoFrameFormat);
            }
        }

        public void SendVideoFrame(long frameId, IntPtr rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat)
        {
            PeerConnection.SendVideoFrame(TrackId, frameId, rgbaPixels, stride, width, height, videoFrameFormat);
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                PeerConnection.LocalVideoFrameEncoded -= OnLocalVideoFrameEncoded;
                LocalVideoFrameEncoded = null;
            }
        }

        protected virtual void OnLocalVideoFrameEncoded(PeerConnection pc, int trackId, long frameId, IntPtr rgbaPixels)
        {
            if (TrackId == trackId)
            {
                LocalVideoFrameEncoded?.Invoke(pc, trackId, frameId, rgbaPixels);
            }
        }
    }
}