using System;

namespace WonderMediaProductions.WebRtc
{
    /// <summary>
    /// TODO: Current a video track must be created before a connection is established!
    /// </summary>
    public class VideoTrack : Disposable
    {
        public int TrackId { get; }

        public PeerConnection PeerConnection { get; }

        // TODO: Must be a rational number
        public int FrameRate { get; }

        public event VideoFrameProcessedDelegate LocalVideoFrameProcessed;

        public VideoTrack(PeerConnection peerConnection, VideoEncoderOptions options)
        {
            PeerConnection = peerConnection;
            FrameRate = options.MaxFramesPerSecond;
            TrackId = peerConnection.AddVideoTrack(options);
            PeerConnection.LocalVideoFrameProcessed += OnLocalVideoFrameProcessed;
        }

        public unsafe void SendVideoFrame(in uint rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat)
        {
            fixed (uint* ptr = &rgbaPixels)
            {
                PeerConnection.SendVideoFrame(TrackId, new IntPtr(ptr), stride, width, height, videoFrameFormat);
            }
        }

        public void SendVideoFrame(IntPtr rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat)
        {
            PeerConnection.SendVideoFrame(TrackId, rgbaPixels, stride, width, height, videoFrameFormat);
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                PeerConnection.LocalVideoFrameProcessed -= OnLocalVideoFrameProcessed;
                LocalVideoFrameProcessed = null;
            }
        }

        protected virtual void OnLocalVideoFrameProcessed(PeerConnection pc, int trackId, IntPtr rgbaPixels, bool isEncoded)
        {
            if (TrackId == trackId)
            {
                LocalVideoFrameProcessed?.Invoke(pc, trackId, rgbaPixels, isEncoded);
            }
        }
    }
}