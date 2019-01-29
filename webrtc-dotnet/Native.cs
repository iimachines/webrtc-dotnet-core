using System;
using System.Runtime.InteropServices;

namespace WonderMediaProductions.WebRtc
{
    public class Native
    {
        internal const string DllPath = "webrtc-native";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LocalDataChannelReadyCallback(string label);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void DataAvailableCallback(string label, string data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void FailureMessageCallback(string msg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void AudioBusReadyCallback(IntPtr data, int bitsPerSample,
            int sampleRate, int numberOfChannels, int numberOfFrames);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void I420FrameReadyCallback(
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            int width, int height, long timeStampUs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LocalSdpReadyToSendCallback(string type, string sdp);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void IceCandidateReadyToSendCallback(string candidate, int sdpMlineIndex, string sdpMid);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SignalingStateChangedCallback(int state);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool Configure(bool hasSignallingThread, bool hasWorkerThread, bool forceSoftwareVideoEncoder);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool PumpQueuedMessages(int timeoutInMS);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern long GetRealtimeClockTimeInMicroseconds();

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CreatePeerConnection(
            string[] turnUrlArray, int turnUrlCount,
            string[] stunUrlArray, int stunUrlCount,
            string username, string credential,
            bool canReceiveAudio, bool canReceiveVideo,
            bool isDtlsSrtpEnabled);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool ClosePeerConnection(IntPtr nativePtr);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool AddStream(IntPtr nativePtr, bool audio, bool video);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool AddDataChannel(IntPtr nativePtr, string label, bool isOrdered, bool isReliable);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CreateOffer(IntPtr nativePtr);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CreateAnswer(IntPtr nativePtr);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SendData(IntPtr nativePtr, string label, string data);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SendVideoFrame(IntPtr nativePtr, in uint rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SetAudioControl(IntPtr nativePtr, bool isMute, bool isRecord);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SetRemoteDescription(IntPtr nativePtr, string type, string sdp);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool AddIceCandidate(IntPtr nativePtr, string sdp, int sdpMlineindex, string sdpMid);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnLocalDataChannelReady(
            IntPtr nativePtr, LocalDataChannelReadyCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnDataFromDataChannelReady(
            IntPtr nativePtr, DataAvailableCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnFailure(IntPtr nativePtr,
            FailureMessageCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnAudioBusReady(IntPtr nativePtr,
            AudioBusReadyCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnLocalI420FrameReady(IntPtr nativePtr,
            I420FrameReadyCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnRemoteI420FrameReady(IntPtr nativePtr,
            I420FrameReadyCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnLocalSdpReadyToSend(IntPtr nativePtr,
            LocalSdpReadyToSendCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnIceCandidateReadyToSend(
            IntPtr nativePtr, IceCandidateReadyToSendCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterSignalingStateChanged(
            IntPtr nativePtr, SignalingStateChangedCallback callback);


        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CanEncodeHardwareTextures();
    }
}