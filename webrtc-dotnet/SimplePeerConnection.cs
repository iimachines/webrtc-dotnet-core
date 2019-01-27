using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace webrtc_dotnet_standard
{
    public class SimplePeerConnection : Disposable
    {
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
        // ReSharper restore NotAccessedField.Local

        private IntPtr _nativePtr;

        /// <summary>
        /// Initializes the threading model, must be called before the first peer connection is created.
        /// </summary>
        public static void InitializeThreading(ThreadingOptions options)
        {
            Check(Native.InitializeThreading(options.UseSignalingThread, options.UseWorkerThread));
        }

        public static void InitializeThreading(Action<ThreadingOptions> configure)
        {
            InitializeThreading(configure.Options());
        }

        public SimplePeerConnection(PeerConnectionOptions options)
        {
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

            Check(_nativePtr != IntPtr.Zero);

            RegisterCallback(out _localDataChannelReadyDelegate, Native.RegisterOnLocalDataChannelReady, RaiseLocalDataChannelReady);
            RegisterCallback(out _dataAvailableDelegate, Native.RegisterOnDataFromDataChannelReady, RaiseDataAvailable);
            RegisterCallback(out _failureMessageDelegate, Native.RegisterOnFailure, RaiseFailureMessage);
            RegisterCallback(out _audioBusReadyDelegate, Native.RegisterOnAudioBusReady, RaiseAudioBusReady);
            RegisterCallback(out _localI420FrameReadyDelegate, Native.RegisterOnLocalI420FrameReady, RaiseLocalVideoFrameReady);
            RegisterCallback(out _remoteI420FrameReadyDelegate, Native.RegisterOnRemoteI420FrameReady, RaiseRemoteVideoFrameReady);
            RegisterCallback(out _localSdpReadyToSendDelegate, Native.RegisterOnLocalSdpReadyToSend, RaiseLocalSdpReadyToSend);
            RegisterCallback(out _iceCandidateReadyToSendDelegate, Native.RegisterOnIceCandidateReadyToSend, RaiseIceCandidateReadyToSend);
            RegisterCallback(out _signalingStateChangedCallback, Native.RegisterSignalingStateChanged, RaiseRegisterSignalingStateChange);
        }

        public SimplePeerConnection(Action<PeerConnectionOptions> configure) : this(configure.Options())
        {
        }

        public override string ToString()
        {
            return _nativePtr.ToPcId();
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

            Native.ClosePeerConnection(ptr);
        }

        public void AddStream(StreamTrack tracks)
        {
            Check(Native.AddStream(_nativePtr, tracks.HasFlag(StreamTrack.Audio), tracks.HasFlag(StreamTrack.Video)));
        }

        public void AddDataChannel(string label, DataChannelFlag flag)
        {
            Check(Native.AddDataChannel(_nativePtr, label,
                flag.HasFlag(DataChannelFlag.Ordered), flag.HasFlag(DataChannelFlag.Reliable)));
        }

        public void CreateOffer()
        {
            Check(Native.CreateOffer(_nativePtr));
        }

        public void CreateAnswer()
        {
            Check(Native.CreateAnswer(_nativePtr));
        }

        public void SendData(string label, string data)
        {
            Check(Native.SendData(_nativePtr, label, data));
        }

        public void SendData(DataMessage msg)
        {
            Check(Native.SendData(_nativePtr, msg.Label, msg.Content));
        }

        public void SendVideoFrameRgba(in uint rgbaPixels, int stride, int width, int height)
        {
            Check(Native.SendVideoFrameRGBA(_nativePtr, rgbaPixels, stride, width, height));
        }

        public void SetAudioControl(bool isMute, bool isRecord)
        {
            Check(Native.SetAudioControl(_nativePtr, isMute, isRecord));
        }

        public void SetRemoteDescription(string type, string sdp)
        {
            Check(Native.SetRemoteDescription(_nativePtr, type, sdp));
        }

        public void SetRemoteDescription(SessionDescription sd)
        {
            Check(Native.SetRemoteDescription(_nativePtr, sd.Type, sd.Sdp));
        }

        public void AddIceCandidate(string candidate, int sdpMlineindex, string sdpMid)
        {
            Check(Native.AddIceCandidate(_nativePtr, candidate, sdpMlineindex, sdpMid));
        }

        public void AddIceCandidate(IceCandidate ice)
        {
            Check(Native.AddIceCandidate(_nativePtr, ice.Candidate, ice.SdpMlineIndex, ice.SdpMid));
        }

        private void RegisterCallback<T>(out T delegateField, Func<IntPtr, T, bool> register, T raiseMethod) where T : Delegate
        {
            delegateField = raiseMethod;
            Check(register(_nativePtr, delegateField));
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

        //public void AddQueuedIceCandidate(IEnumerable<IceCandidate> iceCandidateQueue)
        //{
        //    if (iceCandidateQueue != null)
        //    {
        //        foreach (var ic in iceCandidateQueue)
        //        {
        //            Check(AddIceCandidate(_nativePtr, ic.Candidate, ic.SdpMlineIndex, ic.SdpMid));
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
        public event RegisterSignalingStateChangedDelegate SignalingStateChanged;

        private static void Check(bool result, [CallerMemberName] string caller = null)
        {
            if (!result)
            {
                throw new Exception($"{caller} failed");
            }
        }

        #region Interop

        // Video callbacks.

        #endregion
    }
}