using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WonderMediaProductions.WebRtc
{
    public class PeerConnection : Disposable
    {
        private static int g_LastId;

        // ReSharper disable NotAccessedField.Local
        private readonly Native.AudioBusReadyCallback _audioBusReadyDelegate;
        private readonly Native.DataAvailableCallback _dataAvailableDelegate;
        private readonly Native.FailureMessageCallback _failureMessageDelegate;
        private readonly Native.IceCandidateReadyToSendCallback _iceCandidateReadyToSendDelegate;
        private readonly Native.LocalDataChannelReadyCallback _localDataChannelReadyDelegate;
        private readonly Native.VideoFrameCallback _localVideoFrameDelegate;
        private readonly Native.LocalSdpReadyToSendCallback _localSdpReadyToSendDelegate;
        private readonly Native.VideoFrameCallback _remoteVideoFrameDelegate;
        private readonly Native.StateChangedCallback _signalingStateChangedCallback;
        private readonly Native.StateChangedCallback _connectionStateChangedCallback;
        private readonly Native.VideoFrameProcessedCallback _videoFrameProcessedCallback;
        private readonly Native.RemoteTrackChangedCallback _remoteTrackChangedCallback;

        // ReSharper restore NotAccessedField.Local

        private IntPtr _nativePtr;

        /// <summary>
        /// Configures all peer connections. Must be called before the first peer connection is created.
        /// </summary>
        public static void Configure(GlobalOptions options)
        {
            Native.Check(Native.Configure(
                options.UseSignalingThread,
                options.UseWorkerThread,
                options.ForceSoftwareVideoEncoder,
                options.AutoShutdown,
                options.UseFakeEncoders,
                options.UseFakeDecoders,
                options.LogToStandardError,
                options.LogToDebugOutput,
                options.MinimumLogLevel != TraceLevel.Off ? OnMessageLogged : null,
                4 - (int)(options.MinimumLogLevel)
                ));
        }

        public static event LoggingDelegate MessageLogged;

        private static readonly Native.LoggingCallback OnMessageLogged = (message, severity) =>
            MessageLogged?.Invoke(message.TrimEnd('\n'), (TraceLevel)(4 - severity));

        /// <summary>
        /// This shuts down the global webrtc module.
        /// </summary>
        /// <remarks>
        /// Only needed when you disabled auto-shutdown in the <seealso cref="Configure(GlobalOptions)"/> call />,
        /// and can then only be called after all <see cref="PeerConnection"/> instances are disposed.
        /// </remarks>
        public static void Shutdown()
        {
            Native.Check(Native.Shutdown());
        }

        /// <summary>
        /// Is the peer connection factory created?
        /// This happens after the first peer connection is created,
        /// and stops when the last peer connection is destroyed,
        /// unless auto-shutdown is disabled with <seealso cref="Configure(GlobalOptions)"/>
        /// </summary>
        public static bool HasFactory => Native.HasFactory();

        public static bool SupportsHardwareTextureEncoding => Native.CanEncodeHardwareTextures();

        public PeerConnection(PeerConnectionOptions options)
        {
            Name = options.Name ?? $"PC#{Interlocked.Increment(ref g_LastId)}";

            _nativePtr = Native.CreatePeerConnection(
                options.TurnServers.ToArray(),
                options.TurnServers.Count,
                options.StunServers.ToArray(),
                options.StunServers.Count,
                options.UserName,
                options.PassWord,
                options.CanReceiveAudio,
                options.CanReceiveVideo,
                options.IsDtlsSrtpEnabled);

            Native.Check(_nativePtr != IntPtr.Zero);

            RegisterCallback(out _localDataChannelReadyDelegate, Native.RegisterOnLocalDataChannelReady, RaiseLocalDataChannelReady);
            RegisterCallback(out _dataAvailableDelegate, Native.RegisterOnDataFromDataChannelReady, RaiseDataAvailable);
            RegisterCallback(out _failureMessageDelegate, Native.RegisterOnFailure, RaiseFailureMessage);
            RegisterCallback(out _audioBusReadyDelegate, Native.RegisterOnAudioBusReady, RaiseAudioBusReady);
            RegisterCallback(out _localVideoFrameDelegate, Native.RegisterLocalVideoFrameReady, RaiseLocalVideoFrameReady);
            RegisterCallback(out _remoteVideoFrameDelegate, Native.RegisterRemoteVideoFrameReceived, RaiseRemoteVideoFrameReady);
            RegisterCallback(out _localSdpReadyToSendDelegate, Native.RegisterOnLocalSdpReadyToSend, RaiseLocalSdpReadyToSend);
            RegisterCallback(out _iceCandidateReadyToSendDelegate, Native.RegisterOnIceCandidateReadyToSend, RaiseIceCandidateReadyToSend);
            RegisterCallback(out _signalingStateChangedCallback, Native.RegisterSignalingStateChanged, RaiseSignalingStateChange);
            RegisterCallback(out _connectionStateChangedCallback, Native.RegisterConnectionStateChanged, RaiseConnectionStateChange);
            RegisterCallback(out _videoFrameProcessedCallback, Native.RegisterVideoFrameProcessed, RaiseVideoFrameProcessedDelegate);
            RegisterCallback(out _remoteTrackChangedCallback, Native.RegisterRemoteTrackChanged, RaiseRemoteTrackChanged);
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }

        public IntPtr NativePtr => _nativePtr;

        public static bool PumpQueuedMessages(TimeSpan timeout)
        {
            return Native.PumpQueuedMessages((int)timeout.TotalMilliseconds);
        }

        public static TimeSpan GetRealtimeClockTimeInMicroseconds()
        {
            var timeInMicroseconds = Native.GetRealtimeClockTimeInMicroseconds();
            const long microsecondsPerTick = TimeSpan.TicksPerMillisecond / 1000;
            return TimeSpan.FromTicks(timeInMicroseconds * microsecondsPerTick);
        }

        protected override void OnDispose(bool isDisposing)
        {
            var ptr = Interlocked.Exchange(ref _nativePtr, default);

            // Detach all event handlers, it seems possible we'll get events on another thread while disposing.
            LocalDataChannelReady = null;
            DataAvailable = null;
            FailureMessage = null;
            AudioBusReady = null;
            LocalVideoFrameReady = null;
            RemoteVideoFrameReceived = null;
            LocalSdpReadyToSend = null;
            IceCandidateReadyToSend = null;
            SignalingStateChanged = null;
            ConnectionStateChanged = null;
            LocalVideoFrameProcessed = null;
            RemoteTrackChanged = null;

            Native.ClosePeerConnection(ptr);
        }

        internal int AddVideoTrack(VideoEncoderOptions options)
        {
            var id = Native.AddVideoTrack(_nativePtr, options.Label, options.MinBitsPerSecond, options.MaxBitsPerSecond, options.MaxFramesPerSecond);
            return Native.Check(id);
        }

        public void AddDataChannel(DataChannelOptions options)
        {
            Native.Check(Native.AddDataChannel(_nativePtr, options.Label, options.IsOrdered, options.IsReliable));
        }

        public void RemoveDataChannel(string label)
        {
            Native.Check(Native.RemoveDataChannel(_nativePtr, label));
        }

        public void CreateOffer()
        {
            Native.Check(Native.CreateOffer(_nativePtr));
        }

        public void CreateAnswer()
        {
            Native.Check(Native.CreateAnswer(_nativePtr));
        }

        public unsafe void SendData(string label, in ArraySegment<byte> data, MessageEncoding encoding = MessageEncoding.Binary)
        {
            fixed (byte* startPtr = data.Array)
            {
                var ptr = new IntPtr(startPtr + data.Offset);
                Native.Check(Native.SendData(_nativePtr, label, ptr, data.Count, encoding == MessageEncoding.Binary));
            }
        }

        public void SendData(string label, byte[] bytes, MessageEncoding encoding = MessageEncoding.Binary)
        {
            SendData(label, new ArraySegment<byte>(bytes), encoding);
        }

        public void SendData(string label, string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            SendData(label, bytes, MessageEncoding.Utf8);
        }

        public void SendData(DataMessage msg)
        {
            SendData(msg.Label, msg.Content, msg.Encoding);
        }

        internal void SendVideoFrame(int trackId, IntPtr rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat)
        {
            Native.Check(Native.SendVideoFrame(_nativePtr, trackId, rgbaPixels, stride, width, height, videoFrameFormat));
        }

        public void SetAudioControl(bool isMute, bool isRecord)
        {
            Native.Check(Native.SetAudioControl(_nativePtr, isMute, isRecord));
        }

        public void SetRemoteDescription(string type, string sdp)
        {
            Native.Check(Native.SetRemoteDescription(_nativePtr, type, sdp));
        }

        public void SetRemoteDescription(SessionDescription sd)
        {
            Native.Check(Native.SetRemoteDescription(_nativePtr, sd.Type, sd.Sdp));
        }

        public void AddIceCandidate(string candidate, int sdpMlineindex, string sdpMid)
        {
            Native.Check(Native.AddIceCandidate(_nativePtr, candidate, sdpMlineindex, sdpMid));
        }

        public void AddIceCandidate(IceCandidate ice)
        {
            Native.Check(Native.AddIceCandidate(_nativePtr, ice.Candidate, ice.SdpMlineIndex, ice.SdpMid));
        }

        private void RegisterCallback<T>(out T delegateField, Func<IntPtr, T, bool> register, T raiseMethod) where T : Delegate
        {
            delegateField = raiseMethod;
            Native.Check(register(_nativePtr, delegateField));
        }

        private void RaiseLocalDataChannelReady(string label)
        {
            LocalDataChannelReady?.Invoke(this, label);
        }

        private void RaiseDataAvailable(string label, IntPtr data, int size, bool isBinary)
        {
            byte[] buffer = new byte[size];
            Marshal.Copy(data, buffer, 0, size);
            DataAvailable?.Invoke(this, new DataMessage(
                label,
                new ArraySegment<byte>(buffer, 0, size),
                isBinary ? MessageEncoding.Binary : MessageEncoding.Utf8)
            );
        }

        private void RaiseFailureMessage(string msg)
        {
            FailureMessage?.Invoke(this, msg);
        }

        private void RaiseAudioBusReady(IntPtr data, int bitsPerSample,
            int sampleRate, int numberOfChannels, int numberOfFrames)
        {
            AudioBusReady?.Invoke(this, data, bitsPerSample, sampleRate,
                numberOfChannels, numberOfFrames);
        }

        private void RaiseLocalVideoFrameReady(
            IntPtr texture,
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            int width, int height, long timeStampUs)
        {
            LocalVideoFrameReady?.Invoke(this,
                VideoFrame.FromNative(
                    texture,
                    dataY, dataU, dataV, dataA,
                    strideY, strideU, strideV, strideA,
                    width, height, timeStampUs));
        }

        private void RaiseRemoteVideoFrameReady(
            IntPtr texture,
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            int width, int height, long timeStampUs)
        {
            RemoteVideoFrameReceived?.Invoke(this,
                VideoFrame.FromNative(
                    texture,
                    dataY, dataU, dataV, dataA,
                    strideY, strideU, strideV, strideA,
                    width, height, timeStampUs));
        }

        private void RaiseLocalSdpReadyToSend(string type, string sdp)
        {
            LocalSdpReadyToSend?.Invoke(this, new SessionDescription(type, sdp));
        }

        private void RaiseIceCandidateReadyToSend(string candidate, int sdpMlineIndex, string sdpMid)
        {
            IceCandidateReadyToSend?.Invoke(this, new IceCandidate(candidate, sdpMlineIndex, sdpMid));
        }

        private void RaiseSignalingStateChange(int state)
        {
            SignalingStateChanged?.Invoke(this, (SignalingState)state);
        }

        private void RaiseConnectionStateChange(int state)
        {
            ConnectionStateChanged?.Invoke(this, (ConnectionState)state);
        }

        private void RaiseRemoteTrackChanged(string transceiverMid, int mediaKind, int changeKind)
        {
            RemoteTrackChanged?.Invoke(this, transceiverMid, (TrackMediaKind)mediaKind, (TrackChangeKind)changeKind);
        }

        private void RaiseVideoFrameProcessedDelegate(int trackId, IntPtr rgbaPixels, bool isEncoded)
        {
            LocalVideoFrameProcessed?.Invoke(this, trackId, rgbaPixels, isEncoded);
        }

        //public void AddQueuedIceCandidate(IEnumerable<IceCandidate> iceCandidateQueue)
        //{
        //    if (iceCandidateQueue != null)
        //    {
        //        foreach (var ic in iceCandidateQueue)
        //        {
        //            Native.Check(AddIceCandidate(_nativePtr, ic.Candidate, ic.SdpMlineIndex, ic.SdpMid));
        //        }
        //    }
        //}

        public event LocalDataChannelReadyDelegate LocalDataChannelReady;
        public event DataAvailableDelegate DataAvailable;
        public event FailureMessageDelegate FailureMessage;
        public event AudioBusReadyDelegate AudioBusReady;
        public event VideoFrameReadyDelegate LocalVideoFrameReady;
        public event VideoFrameReadyDelegate RemoteVideoFrameReceived;
        public event LocalSdpReadyToSendDelegate LocalSdpReadyToSend;
        public event IceCandidateReadyToSendDelegate IceCandidateReadyToSend;
        public event SignalingStateChangedDelegate SignalingStateChanged;
        public event ConnectionStateChangedDelegate ConnectionStateChanged;
        public event VideoFrameProcessedDelegate LocalVideoFrameProcessed;
        public event RemoteTrackChangedDelegate RemoteTrackChanged;
    }
}