using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace WonderMediaProductions.WebRtc
{
    public class ObservablePeerConnection : PeerConnection
    {
	    private bool _canConnect = true;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<SessionDescription> _localSessionDescriptionStream = new Subject<SessionDescription>();
        private readonly Subject<IceCandidate> _localIceCandidateStream = new Subject<IceCandidate>();
        private readonly BehaviorSubject<ConnectionState> _connectionStateStream = new BehaviorSubject<ConnectionState>(ConnectionState.Closed);
        private readonly BehaviorSubject<SignalingState> _signalingStateStream = new BehaviorSubject<SignalingState>(SignalingState.Closed);

		private readonly Subject<DataMessage> _receivedDataStream = new Subject<DataMessage>();
        private readonly Subject<VideoFrame> _receivedVideoStream = new Subject<VideoFrame>();
        private readonly Subject<VideoFrameMessage> _localVideoFrameProcessedStream = new Subject<VideoFrameMessage>();
        private readonly Subject<RemoteTrackChange> _remoteTrackChangeStream = new Subject<RemoteTrackChange>();
        private readonly Subject<string> _failureMessageStream = new Subject<string>();

		public IObservable<SessionDescription> LocalSessionDescriptionStream => _localSessionDescriptionStream;
        public IObservable<IceCandidate> LocalIceCandidateStream => _localIceCandidateStream;
        public IObservable<SignalingState> SignalingStateStream => _signalingStateStream;
        public IObservable<ConnectionState> ConnectionStateStream => _connectionStateStream;

        public IObservable<DataMessage> ReceivedDataStream => _receivedDataStream;
        public IObservable<VideoFrame> ReceivedVideoStream => _receivedVideoStream;

        public IObservable<RemoteTrackChange> RemoteTrackChangeStream => _remoteTrackChangeStream;

        public IObservable<VideoFrameMessage> LocalVideoFrameProcessedStream => _localVideoFrameProcessedStream;

        public IObservable<string> FailureMessageStream => _failureMessageStream;

		public ObservablePeerConnection(PeerConnectionOptions options) : base(options)
        {
	        _disposables.Add(_localSessionDescriptionStream);
	        _disposables.Add(_localIceCandidateStream);
	        _disposables.Add(_receivedDataStream);
	        _disposables.Add(_receivedVideoStream);
	        _disposables.Add(_localVideoFrameProcessedStream);
	        _disposables.Add(_remoteTrackChangeStream);
        }

		public SignalingState SignalingState => _signalingStateStream.Value;

        public void Connect(
            IObservable<DataMessage> outgoingMessages,
            IObservable<SessionDescription> receivedSessionDescriptions,
            IObservable<IceCandidate> receivedIceCandidates)
        {
	        if (!_canConnect)
				throw new Exception($"{GetType().Name}.{nameof(Connect)} can only be called once!");

	        _canConnect = false;

			LocalDataChannelReady += (pc, label) =>
            {
                DebugLog($"{Name} is ready to send data on channel '{label}'");
                _disposables.Add(outgoingMessages.Where(data => data.Label == label).Subscribe(SendData));
            };

            DataAvailable += (pc, msg) =>
            {
                DebugLog($"{Name} received data: {msg}");
                _receivedDataStream.TryOnNext(msg);
            };

            LocalSdpReadyToSend += (pc, sd) =>
            {
                DebugLog($"{Name} received local session description: {sd}");
                _localSessionDescriptionStream.TryOnNext(sd);
            };

            IceCandidateReadyToSend += (pc, ice) =>
            {
                DebugLog($"{Name} received local ice candidate: {ice}");
                _localIceCandidateStream.TryOnNext(ice);
            };

            RemoteTrackChanged += (pc, transceiverMid, mediaKind, changeKind) =>
            {
                DebugLog($"{Name} transceiver {transceiverMid} {mediaKind} track {changeKind}");
                _remoteTrackChangeStream.TryOnNext(new RemoteTrackChange(transceiverMid, mediaKind, changeKind));
            };

            RemoteVideoFrameReceived += (pc, frame) => 
                _receivedVideoStream.TryOnNext(frame);

            LocalVideoFrameProcessed += (pc, trackId, pixels, isEncoded) =>
                _localVideoFrameProcessedStream.TryOnNext(new VideoFrameMessage(trackId, pixels, isEncoded));

            SignalingStateChanged += (pc, state) =>
            {
                DebugLog($"{Name} signaling state changed: {state}");
                _signalingStateStream.TryOnNext(state);

                if (state == SignalingState.HaveRemoteOffer)
                {
                    CreateAnswer();
                }
            };

            ConnectionStateChanged += (pc, state) =>
            {
                DebugLog($"{Name} connection state changed: {state}");
                _connectionStateStream.TryOnNext(state);
            };

            _disposables.Add(receivedIceCandidates.Subscribe(ice =>
            {
                DebugLog($"{Name} received remote ICE candidate: {ice}");
                AddIceCandidate(ice);
            }));

            _disposables.Add(receivedSessionDescriptions.Subscribe(sd =>
            {
                DebugLog($"{Name} received remote session description: {sd}");
                SetRemoteDescription(sd);
            }));
        }

        [Conditional("DEBUG")]
        private void DebugLog(string msg)
        {
            Console.WriteLine(msg);
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _disposables?.Dispose();
            }

            base.OnDispose(isDisposing);
        }
    }
}
