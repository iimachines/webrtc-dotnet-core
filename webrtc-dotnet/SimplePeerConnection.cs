using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace webrtc_dotnet_standard
{
    public class SimplePeerConnection : IDisposable
    {
        // ReSharper disable NotAccessedField.Local
        private readonly AudioBusReadyInternalDelegate _audioBusReadyDelegate;
        private readonly DataAvailableInternalDelegate _dataAvailableDelegate;
        private readonly FailureMessageInternalDelegate _failureMessageDelegate;
        private readonly IceCandidateReadyToSendInternalDelegate _iceCandidateReadyToSendDelegate;
        private readonly LocalDataChannelReadyInternalDelegate _localDataChannelReadyDelegate;
        private readonly I420FrameReadyInternalDelegate _localI420FrameReadyDelegate;
        private readonly LocalSdpReadyToSendInternalDelegate _localSdpReadyToSendDelegate;
        private I420FrameReadyInternalDelegate _remoteI420FrameReadyDelegate;
        // ReSharper restore NotAccessedField.Local

        private IntPtr _nativePtr;

        /// <summary>
        /// Initializes the threading model, must be called before the first peer connection is created.
        /// </summary>
        public static void InitializeThreading(ThreadingOptions options)
        {
            Check(InitializeThreading(options.UseSignalingThread, options.UseWorkerThread));
        }

        public static void InitializeThreading(Action<ThreadingOptions> configure)
        {
            InitializeThreading(configure.Options());
        }

        public SimplePeerConnection(PeerConnectionOptions options)
        {
            _nativePtr = CreatePeerConnection(
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

            RegisterCallback(out _localDataChannelReadyDelegate, RegisterOnLocalDataChannelReady, RaiseLocalDataChannelReady);
            RegisterCallback(out _dataAvailableDelegate, RegisterOnDataFromDataChannelReady, RaiseDataAvailable);
            RegisterCallback(out _failureMessageDelegate, RegisterOnFailure, RaiseFailureMessage);
            RegisterCallback(out _audioBusReadyDelegate, RegisterOnAudioBusReady, RaiseAudioBusReady);
            RegisterCallback(out _localI420FrameReadyDelegate, RegisterOnLocalI420FrameReady, RaiseLocalVideoFrameReady);
            RegisterCallback(out _remoteI420FrameReadyDelegate, RegisterOnRemoteI420FrameReady, RaiseRemoteVideoFrameReady);
            RegisterCallback(out _localSdpReadyToSendDelegate, RegisterOnLocalSdpReadyToSend, RaiseLocalSdpReadyToSend);
            RegisterCallback(out _iceCandidateReadyToSendDelegate, RegisterOnIceCandidateReadyToSend, RaiseIceCandidateReadyToSend);
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
            return PumpQueuedMessages((int)timeout.TotalMilliseconds);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
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

            ClosePeerConnection(ptr);
        }

        ~SimplePeerConnection()
        {
            Dispose(false);
        }

        public void AddStream(bool audioOnly)
        {
            Check(AddStream(_nativePtr, audioOnly));
        }

        public void AddDataChannel(string label, DataChannelFlag flag)
        {
            Check(AddDataChannel(_nativePtr, label,
                flag.HasFlag(DataChannelFlag.Ordered), flag.HasFlag(DataChannelFlag.Reliable)));
        }

        public void CreateOffer()
        {
            Check(CreateOffer(_nativePtr));
        }

        public void CreateAnswer()
        {
            Check(CreateAnswer(_nativePtr));
        }

        public void SendData(string label, string data)
        {
            Check(SendData(_nativePtr, label, data));
        }

        public void SendData(DataMessage msg)
        {
            Check(SendData(_nativePtr, msg.Label, msg.Content));
        }

        public void SetAudioControl(bool isMute, bool isRecord)
        {
            Check(SetAudioControl(_nativePtr, isMute, isRecord));
        }

        public void SetRemoteDescription(string type, string sdp)
        {
            Check(SetRemoteDescription(_nativePtr, type, sdp));
        }

        public void SetRemoteDescription(SessionDescription sd)
        {
            Check(SetRemoteDescription(_nativePtr, sd.Type, sd.Content));
        }

        public void AddIceCandidate(string candidate, int sdpMlineindex, string sdpMid)
        {
            Check(AddIceCandidate(_nativePtr, candidate, sdpMlineindex, sdpMid));
        }

        public void AddIceCandidate(IceCandidate ice)
        {
            Check(AddIceCandidate(_nativePtr, ice.Candidate, ice.SdpMlineIndex, ice.SdpMid));
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

        #region Interop
        private static void Check(bool result, [CallerMemberName] string caller = null)
        {
            if (!result)
            {
                throw new Exception($"{caller} failed");
            }
        }

        private const string DllPath = "webrtc-native";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LocalDataChannelReadyInternalDelegate(string label);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DataAvailableInternalDelegate(string label, string data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FailureMessageInternalDelegate(string msg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void AudioBusReadyInternalDelegate(IntPtr data, int bitsPerSample,
            int sampleRate, int numberOfChannels, int numberOfFrames);

        // Video callbacks.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void I420FrameReadyInternalDelegate(
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            int width, int height, long timeStampUs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LocalSdpReadyToSendInternalDelegate(string type, string sdp);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void IceCandidateReadyToSendInternalDelegate(
            string candidate, int sdpMlineIndex, string sdpMid);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InitializeThreading(bool hasSignallingThread, bool hasWorkerThread);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool PumpQueuedMessages(int timeoutInMS);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreatePeerConnection(
            string[] turnUrlArray, int turnUrlCount,
            string[] stunUrlArray, int stunUrlCount,
            string username, string credential,
            bool canReceiveAudio, bool canReceiveVideo,
            bool isDtlsSrtpEnabled);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ClosePeerConnection(IntPtr nativePtr);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool AddStream(IntPtr nativePtr, bool audioOnly);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool AddDataChannel(IntPtr nativePtr, string label, bool isOrdered, bool isReliable);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool CreateOffer(IntPtr nativePtr);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool CreateAnswer(IntPtr nativePtr);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SendData(IntPtr nativePtr, string label, string data);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetAudioControl(IntPtr nativePtr, bool isMute, bool isRecord);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RegisterOnLocalDataChannelReady(
            IntPtr nativePtr, LocalDataChannelReadyInternalDelegate callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RegisterOnDataFromDataChannelReady(
            IntPtr nativePtr, DataAvailableInternalDelegate callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RegisterOnFailure(IntPtr nativePtr,
            FailureMessageInternalDelegate callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RegisterOnAudioBusReady(IntPtr nativePtr,
            AudioBusReadyInternalDelegate callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RegisterOnLocalI420FrameReady(IntPtr nativePtr,
            I420FrameReadyInternalDelegate callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RegisterOnRemoteI420FrameReady(IntPtr nativePtr,
            I420FrameReadyInternalDelegate callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RegisterOnLocalSdpReadyToSend(IntPtr nativePtr,
            LocalSdpReadyToSendInternalDelegate callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RegisterOnIceCandidateReadyToSend(
            IntPtr nativePtr, IceCandidateReadyToSendInternalDelegate callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetRemoteDescription(IntPtr nativePtr, string type, string sdp);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool AddIceCandidate(IntPtr nativePtr, string sdp, int sdpMlineindex, string sdpMid);
        #endregion
    }
}