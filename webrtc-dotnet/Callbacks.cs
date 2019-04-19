using System;
using System.Diagnostics;

namespace WonderMediaProductions.WebRtc
{
    public enum TrackMediaKind
    {
        Audio,
        Video,
        Data,
    }

    public enum TrackChangeKind
    {
        Changed,
        Removed,
        Stopped
    }

    public delegate void LoggingDelegate(string message, TraceLevel severity);

    public delegate void AudioBusReadyDelegate(PeerConnection pc, IntPtr data, int bitsPerSample,
        int sampleRate, int numberOfChannels, int numberOfFrames);

    public delegate void DataAvailableDelegate(PeerConnection pc, DataMessage msg);

    public delegate void FailureMessageDelegate(PeerConnection pc, string msg);

    public delegate void VideoFrameReadyDelegate(PeerConnection pc, VideoFrame frame);

    public delegate void IceCandidateReadyToSendDelegate(PeerConnection pc, IceCandidate ice);

    public delegate void LocalDataChannelReadyDelegate(PeerConnection pc, string label);

    public delegate void LocalSdpReadyToSendDelegate(PeerConnection pc, SessionDescription sd);

    public delegate void SignalingStateChangedDelegate(PeerConnection pc, SignalingState state);

    public delegate void ConnectionStateChangedDelegate(PeerConnection pc, ConnectionState state);

    public delegate void VideoFrameProcessedDelegate(PeerConnection pc, int trackId, IntPtr rgbaPixels, bool isEncoded);

    public delegate void RemoteTrackChangedDelegate(PeerConnection pc, string transceiverMid, TrackMediaKind mediaKind, TrackChangeKind changeKind);
}
