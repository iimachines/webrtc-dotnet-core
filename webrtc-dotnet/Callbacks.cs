using System;

namespace webrtc_dotnet_standard
{
    public delegate void AudioBusReadyDelegate(SimplePeerConnection pc, IntPtr data, int bitsPerSample,
        int sampleRate, int numberOfChannels, int numberOfFrames);

    public delegate void DataAvailableDelegate(SimplePeerConnection pc, DataMessage msg);

    public delegate void FailureMessageDelegate(SimplePeerConnection pc, string msg);

    public delegate void I420FrameReadyDelegate(SimplePeerConnection pc, VideoFrameYuvAlpha frame);

    public delegate void IceCandidateReadyToSendDelegate(SimplePeerConnection pc, IceCandidate ice);

    public delegate void LocalDataChannelReadyDelegate(SimplePeerConnection pc, string label);

    public delegate void LocalSdpReadyToSendDelegate(SimplePeerConnection pc, SessionDescription sd);

}
