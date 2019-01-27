using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace webrtc_dotnet_standard
{

    public class ObservablePeerConnection : Disposable
    {
        private readonly PeerConnectionOptions _options;

        private SimplePeerConnection _connection;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<SessionDescription> _localSessionDescriptionStream = new Subject<SessionDescription>();
        private readonly Subject<IceCandidate> _localIceCandidateStream = new Subject<IceCandidate>();
        private readonly BehaviorSubject<SignalingState> _signalingStateStream = new BehaviorSubject<SignalingState>(SignalingState.Closed);

        private readonly Subject<DataMessage> _receivedDataStream = new Subject<DataMessage>();
        private readonly Subject<VideoFrameYuvAlpha> _receivedVideoStream = new Subject<VideoFrameYuvAlpha>();

        public IObservable<SessionDescription> LocalSessionDescriptionStream => _localSessionDescriptionStream;
        public IObservable<IceCandidate> LocalIceCandidateStream => _localIceCandidateStream;
        public IObservable<SignalingState> SignalingStateStream => _signalingStateStream;

        public IObservable<DataMessage> ReceivedDataStream => _receivedDataStream;
        public IObservable<VideoFrameYuvAlpha> ReceivedVideoStream => _receivedVideoStream;

        public ObservablePeerConnection(string name, PeerConnectionOptions options)
        {
            Name = name;
            _options = options;
        }

        public ObservablePeerConnection(string name, Action<PeerConnectionOptions> configure)
            : this(name, configure.Options())
        {
        }

        public string Name { get; }

        public SignalingState SignalingState => _signalingStateStream.Value;

        public void Connect(
            IObservable<DataMessage> outgoingMessages,
            IObservable<SessionDescription> receivedSessionDescriptions,
            IObservable<IceCandidate> receivedIceCandidates)
        {
            _disposables.Add(_localSessionDescriptionStream);
            _disposables.Add(_localIceCandidateStream);

            _disposables.Add(_receivedDataStream);
            _disposables.Add(_receivedVideoStream);

            DebugLog($"Creating {Name}...");
            _connection = new SimplePeerConnection(_options);

            DebugLog($"{Name} has id {_connection}");

            _connection.LocalDataChannelReady += (pc, label) =>
            {
                DebugLog($"{Name} is ready to send data on channel '{label}'");
                _disposables.Add(outgoingMessages.Where(data => data.Label == label).Subscribe(_connection.SendData));
            };

            _connection.DataAvailable += (pc, msg) =>
            {
                DebugLog($"{Name} received data: {msg}");
                _receivedDataStream.OnNext(msg);
            };

            _connection.LocalSdpReadyToSend += (pc, sd) =>
            {
                DebugLog($"{Name} received local session description: {sd}");
                _localSessionDescriptionStream.OnNext(sd);
            };

            _connection.IceCandidateReadyToSend += (pc, ice) =>
            {
                DebugLog($"{Name} received local ice candidate: {ice}");
                _localIceCandidateStream.OnNext(ice);
            };

            _connection.RemoteVideoFrameReady += (pc, frame) => { _receivedVideoStream.OnNext(frame); };

            _connection.SignalingStateChanged += (pc, state) =>
            {
                DebugLog($"{Name} signaling state changed: {state}");
                _signalingStateStream.OnNext(state);

                if (SignalingState == SignalingState.HaveRemoteOffer)
                {
                    _connection.CreateAnswer();
                }
            };

            _disposables.Add(receivedIceCandidates.Subscribe(ice =>
            {
                DebugLog($"{Name} received remote ICE candidate: {ice}");
                _connection.AddIceCandidate(ice);
            }));

            _disposables.Add(receivedSessionDescriptions.Subscribe(sd =>
            {
                DebugLog($"{Name} received remote session description: {sd}");
                _connection.SetRemoteDescription(sd);
            }));
        }

        [Conditional("DEBUG")]
        private void DebugLog(string msg)
        {
            Console.WriteLine(msg);
        }

        public void AddStream(StreamTrack tracks)
        {
            _connection.AddStream(tracks);
        }

        public void AddDataChannel(string label, DataChannelFlag flag)
        {
            _connection.AddDataChannel(label, flag);
        }

        public void SendVideoFrameRgba(in uint rgbaPixels, int stride, int width, int height)
        {
            _connection.SendVideoFrameRgba(rgbaPixels, stride, width, height);
        }

        public void CreateOffer()
        {
            _connection.CreateOffer();
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // First dispose the connection to break all event handlers
                _connection?.Dispose();

                // Then dispose the rest.
                _disposables?.Dispose();
            }
        }
    }
}