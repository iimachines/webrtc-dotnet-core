using System;
using System.Diagnostics;

namespace WonderMediaProductions.WebRtc
{
    public delegate void LoggingDelegate(string message, TraceLevel severity);

    public delegate void AudioBusReadyDelegate(PeerConnection pc, IntPtr data, int bitsPerSample,
        int sampleRate, int numberOfChannels, int numberOfFrames);

    public delegate void DataAvailableDelegate(PeerConnection pc, DataMessage msg);

    public delegate void FailureMessageDelegate(PeerConnection pc, string msg);

    public delegate void I420FrameReadyDelegate(PeerConnection pc, VideoFrameYuvAlpha frame);

    public delegate void IceCandidateReadyToSendDelegate(PeerConnection pc, IceCandidate ice);

    public delegate void LocalDataChannelReadyDelegate(PeerConnection pc, string label);

    public delegate void LocalSdpReadyToSendDelegate(PeerConnection pc, SessionDescription sd);

    public delegate void SignalingStateChangedDelegate(PeerConnection pc, SignalingState state);

    public delegate void VideoFrameEncodedDelegate(PeerConnection pc, int trackId, IntPtr rgbaPixels);
}
