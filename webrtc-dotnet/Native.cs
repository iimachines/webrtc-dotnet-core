using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WonderMediaProductions.WebRtc
{
    public class Native
    {
        internal const string DllPath = "webrtc-native";

        public static void Check(bool result, [CallerMemberName] string caller = null)
        {
            if (!result)
            {
                throw new Exception($"{caller} failed");
            }
        }

        public static int Check(int id, [CallerMemberName] string caller = null)
        {
            if (id == 0)
            {
                throw new Exception($"{caller} failed");
            }

            return id;
        }

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
        internal static extern bool ClosePeerConnection(IntPtr connection);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AddVideoTrack(IntPtr connection, string label, int minBitsPerSecond, int maxBitsPerSeconds, int maxFramesPerSecond);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool AddDataChannel(IntPtr connection, string label, bool isOrdered, bool isReliable);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CreateOffer(IntPtr connection);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CreateAnswer(IntPtr connection);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SendData(IntPtr connection, string label, string data);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SendVideoFrame(IntPtr connection, int trackId, in uint rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SetAudioControl(IntPtr connection, bool isMute, bool isRecord);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SetRemoteDescription(IntPtr connection, string type, string sdp);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool AddIceCandidate(IntPtr connection, string sdp, int sdpMlineindex, string sdpMid);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnLocalDataChannelReady(
            IntPtr connection, LocalDataChannelReadyCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnDataFromDataChannelReady(
            IntPtr connection, DataAvailableCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnFailure(IntPtr connection,
            FailureMessageCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnAudioBusReady(IntPtr connection,
            AudioBusReadyCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnLocalI420FrameReady(IntPtr connection,
            I420FrameReadyCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnRemoteI420FrameReady(IntPtr connection,
            I420FrameReadyCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnLocalSdpReadyToSend(IntPtr connection,
            LocalSdpReadyToSendCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnIceCandidateReadyToSend(
            IntPtr connection, IceCandidateReadyToSendCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterSignalingStateChanged(
            IntPtr connection, SignalingStateChangedCallback callback);


        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CanEncodeHardwareTextures();
    }
}