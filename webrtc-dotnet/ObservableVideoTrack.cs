using System;
using System.Reactive.Subjects;

namespace WonderMediaProductions.WebRtc
{
    public class ObservableVideoTrack : VideoTrack
    {
        private readonly Subject<VideoFrameMessage> _localVideoFrameProcessedStream = new Subject<VideoFrameMessage>();

        public new ObservablePeerConnection PeerConnection => (ObservablePeerConnection) base.PeerConnection;

        public ObservableVideoTrack(ObservablePeerConnection peerConnection, VideoEncoderOptions options) 
            : base(peerConnection, options)
        {
        }

        public IObservable<VideoFrameMessage> LocalVideoFrameProcessedStream => _localVideoFrameProcessedStream;

        protected override void OnLocalVideoFrameProcessed(PeerConnection pc, int trackId, IntPtr rgbaPixels, bool isEncoded)
        {
            _localVideoFrameProcessedStream.TryOnNext(new VideoFrameMessage(trackId, rgbaPixels, isEncoded));
            base.OnLocalVideoFrameProcessed(pc, trackId, rgbaPixels, isEncoded);
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _localVideoFrameProcessedStream.Dispose();
            }

            base.OnDispose(isDisposing);
        }
    }
}