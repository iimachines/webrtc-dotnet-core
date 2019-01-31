using System;
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
        private readonly Native.I420FrameReadyCallback _localI420FrameReadyDelegate;
        private readonly Native.LocalSdpReadyToSendCallback _localSdpReadyToSendDelegate;
        private readonly Native.I420FrameReadyCallback _remoteI420FrameReadyDelegate;
        private readonly Native.SignalingStateChangedCallback _signalingStateChangedCallback;
        private readonly Native.VideoFrameEncodedCallback _videoFrameEncodedCallback;

        // ReSharper restore NotAccessedField.Local

        private IntPtr _nativePtr;

        /// <summary>
        /// Configures all peer connections. Must be called before the first peer connection is created.
        /// </summary>
        public static void Configure(GlobalOptions options)
        {
            Native.Check(Native.Configure(options.UseSignalingThread, options.UseWorkerThread, options.ForceSoftwareVideoEncoder));
        }

        public static void Configure(Action<GlobalOptions> configure)
        {
            Configure(configure.Options());
        }

        public static bool SupportsHardwareTextureEncoding => Native.CanEncodeHardwareTextures();

        public PeerConnection(PeerConnectionOptions options)
        {
            Name = options.Name ?? $"PC#${Interlocked.Increment(ref g_LastId)}";

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
            RegisterCallback(out _localI420FrameReadyDelegate, Native.RegisterOnLocalI420FrameReady, RaiseLocalVideoFrameReady);
            RegisterCallback(out _remoteI420FrameReadyDelegate, Native.RegisterOnRemoteI420FrameReady, RaiseRemoteVideoFrameReady);
            RegisterCallback(out _localSdpReadyToSendDelegate, Native.RegisterOnLocalSdpReadyToSend, RaiseLocalSdpReadyToSend);
            RegisterCallback(out _iceCandidateReadyToSendDelegate, Native.RegisterOnIceCandidateReadyToSend, RaiseIceCandidateReadyToSend);
            RegisterCallback(out _signalingStateChangedCallback, Native.RegisterSignalingStateChanged, RaiseRegisterSignalingStateChange);
            RegisterCallback(out _videoFrameEncodedCallback, Native.RegisterVideoFrameEncoded, RaiseVideoFrameEncodedDelegate);
        }

        public PeerConnection(Action<PeerConnectionOptions> configure) : this(configure.Options())
        {
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
            RemoteVideoFrameReady = null;
            LocalSdpReadyToSend = null;
            IceCandidateReadyToSend = null;
            SignalingStateChanged = null;
            LocalVideoFrameEncoded = null;

            Native.ClosePeerConnection(ptr);
        }

        internal int RegisterVideoTrack(VideoEncoderOptions options)
        {
            var id = Native.AddVideoTrack(_nativePtr, options.Label, options.MinBitsPerSecond, options.MaxBitsPerSecond, options.MaxFramesPerSecond);
            return Native.Check(id);
        }

        [Obsolete("TODO: Will be replaced by a DataChannel class, like the VideoTrack")]
        public void AddDataChannel(string label, DataChannelFlag flag)
        {
            Native.Check(Native.AddDataChannel(_nativePtr, label,
                flag.HasFlag(DataChannelFlag.Ordered), flag.HasFlag(DataChannelFlag.Reliable)));
        }

        public void CreateOffer()
        {
            Native.Check(Native.CreateOffer(_nativePtr));
        }

        public void CreateAnswer()
        {
            Native.Check(Native.CreateAnswer(_nativePtr));
        }

        public void SendData(string label, string data)
        {
            Native.Check(Native.SendData(_nativePtr, label, data));
        }

        public void SendData(DataMessage msg)
        {
            Native.Check(Native.SendData(_nativePtr, msg.Label, msg.Content));
        }

        internal void SendVideoFrame(int trackId, long frameId, IntPtr rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat)
        {
            Native.Check(Native.SendVideoFrame(_nativePtr, trackId, frameId, rgbaPixels, stride, width, height, videoFrameFormat));
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

        private void RaiseDataAvailable(string label, string data)
        {
            DataAvailable?.Invoke(this, new DataMessage(label, data));
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
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            int width, int height, long timeStampUs)
        {
            LocalVideoFrameReady?.Invoke(this, new VideoFrameYuvAlpha(
                dataY, dataU, dataV, dataA,
                strideY, strideU, strideV, strideA,
                width, height, timeStampUs));
        }

        private void RaiseRemoteVideoFrameReady(
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            int width, int height, long timeStampUs)
        {
            RemoteVideoFrameReady?.Invoke(this, new VideoFrameYuvAlpha(
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

        private void RaiseRegisterSignalingStateChange(int state)
        {
            SignalingStateChanged?.Invoke(this, (SignalingState)state);
        }

        private void RaiseVideoFrameEncodedDelegate(int trackId, long frameId, IntPtr rgbaPixels)
        {
            LocalVideoFrameEncoded?.Invoke(this, trackId, frameId, rgbaPixels);
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
        public event I420FrameReadyDelegate LocalVideoFrameReady;
        public event I420FrameReadyDelegate RemoteVideoFrameReady;
        public event LocalSdpReadyToSendDelegate LocalSdpReadyToSend;
        public event IceCandidateReadyToSendDelegate IceCandidateReadyToSend;
        public event SignalingStateChangedDelegate SignalingStateChanged;
        public event VideoFrameEncodedDelegate LocalVideoFrameEncoded;
    }
}