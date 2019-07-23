using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WonderMediaProductions.WebRtc
{
    public class Native
    {
        internal const string DllPath = "webrtc-native";

        static Native()
        {
            if (!Environment.Is64BitProcess)
                throw new NotSupportedException("webrtc-dotnet-core only supports 64-bit processes");
        }

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
        internal delegate void DataAvailableCallback(string label, IntPtr data, int size, bool isBinary);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void FailureMessageCallback(string msg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void AudioBusReadyCallback(IntPtr data, int bitsPerSample,
            int sampleRate, int numberOfChannels, int numberOfFrames);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void VideoFrameCallback(
            IntPtr texture,
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            int width, int height, long timeStampUs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LocalSdpReadyToSendCallback(string type, string sdp);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void IceCandidateReadyToSendCallback(string candidate, int sdpMlineIndex, string sdpMid);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void StateChangedCallback(int state);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void VideoFrameProcessedCallback(int videoTrackId, IntPtr rgbaPixels, bool isEncoded);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void RemoteTrackChangedCallback(string transceiverMid, int mediaKind, int changeKind);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LoggingCallback(string message, int severity);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool Configure(
            bool hasSignallingThread,
            bool hasWorkerThread,
            bool forceSoftwareVideoEncoder,
            bool autoShutdown,
            bool useFakeEncoders,
            bool useFakeDecoders,
            bool logToStdErr,
            bool logToDebug,
            LoggingCallback loggingCallback,
            int minimumLoggingSeverity,
            int startBitrate);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool Shutdown();

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool HasFactory();

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool PumpQueuedMessages(int timeoutInMS);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern long GetRealtimeClockTimeInMicroseconds();

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CreatePeerConnection(
            string[] iceUrlArray, int iceUrlCount,
            string iceUsername, string icePassword,
            bool canReceiveAudio, bool canReceiveVideo,
            bool isDtlsSrtpEnabled);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool ClosePeerConnection(IntPtr connection);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AddVideoTrack(IntPtr connection, string label, int minBitsPerSecond, int maxBitsPerSeconds, int maxFramesPerSecond);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool AddDataChannel(IntPtr connection, string label, bool isOrdered, bool isReliable);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RemoveDataChannel(IntPtr connection, string label);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CreateOffer(IntPtr connection);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CreateAnswer(IntPtr connection);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SendData(IntPtr connection, string label, IntPtr data, int length, bool isBinary);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SendVideoFrame(IntPtr connection, int trackId, IntPtr rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat);

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
        internal static extern bool RegisterLocalVideoFrameReady(IntPtr connection,
            VideoFrameCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterRemoteVideoFrameReceived(IntPtr connection,
            VideoFrameCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnLocalSdpReadyToSend(IntPtr connection,
            LocalSdpReadyToSendCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterOnIceCandidateReadyToSend(
            IntPtr connection, IceCandidateReadyToSendCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterSignalingStateChanged(
            IntPtr connection, StateChangedCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterConnectionStateChanged(
            IntPtr connection, StateChangedCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterVideoFrameProcessed(
            IntPtr connection, VideoFrameProcessedCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RegisterRemoteTrackChanged(
            IntPtr connection, RemoteTrackChangedCallback callback);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CanEncodeHardwareTextures();
    }
}