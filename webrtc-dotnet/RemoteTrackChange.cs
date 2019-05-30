namespace WonderMediaProductions.WebRtc
{
    public sealed class RemoteTrackChange
    {
        /// <summary>
        /// Can be string.Empty during negotiation
        /// </summary>
        public readonly string TransceiverMid;

        public readonly TrackMediaKind MediaKind;

        public readonly TrackChangeKind ChangeKind;

        public RemoteTrackChange(string transceiverMid, TrackMediaKind mediaKind, TrackChangeKind changeKind)
        {
            TransceiverMid = transceiverMid ?? string.Empty;
            MediaKind = mediaKind;
            ChangeKind = changeKind;
        }

        public override string ToString()
        {
	        return $"{nameof(TransceiverMid)}: {TransceiverMid}, {nameof(MediaKind)}: {MediaKind}, {nameof(ChangeKind)}: {ChangeKind}";
        }
    }
}
