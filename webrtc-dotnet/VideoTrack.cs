namespace WonderMediaProductions.WebRtc
{
    public sealed class VideoTrack
    {
        public readonly int Id;

        public readonly PeerConnection PeerConnection;


        public VideoTrack(PeerConnection peerConnection, int id)
        {
            PeerConnection = peerConnection;
            Id = id;
        }

        public void SendVideoFrame(in uint rgbaPixels, int stride, int width, int height, VideoFrameFormat videoFrameFormat)
        {
            PeerConnection.SendVideoFrame(Id, in rgbaPixels, stride, width, height, videoFrameFormat);
        }
    }
}