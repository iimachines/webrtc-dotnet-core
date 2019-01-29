namespace WonderMediaProductions.WebRtc
{
    public class GlobalOptions
    {
        public bool UseSignalingThread = true;
        public bool UseWorkerThread = true;
        public bool ForceSoftwareVideoEncoder = false;

        public bool IsSingleThreaded
        {
            set => UseSignalingThread = UseWorkerThread = value;
        }
    }
}