using System;
using System.Reactive.Subjects;

namespace WonderMediaProductions.WebRtc
{
    public class ObservableVideoTrack : VideoTrack
    {
        private readonly Subject<VideoFrameMessage> _localVideoFrameEncodedStream = new Subject<VideoFrameMessage>();

        public new ObservablePeerConnection PeerConnection => (ObservablePeerConnection) base.PeerConnection;

        public ObservableVideoTrack(ObservablePeerConnection peerConnection, VideoEncoderOptions options) 
            : base(peerConnection, options)
        {
        }

        public IObservable<VideoFrameMessage> LocalVideoFrameEncodedStream => _localVideoFrameEncodedStream;

        protected override void OnLocalVideoFrameEncoded(PeerConnection pc, int trackId, IntPtr rgbaPixels)
        {
            _localVideoFrameEncodedStream.OnNext(new VideoFrameMessage(trackId, rgbaPixels));
            base.OnLocalVideoFrameEncoded(pc, trackId, rgbaPixels);
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _localVideoFrameEncodedStream.Dispose();
            }

            base.OnDispose(isDisposing);
        }
    }
}