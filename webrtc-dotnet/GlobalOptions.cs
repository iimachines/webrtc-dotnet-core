using System.Diagnostics;

namespace WonderMediaProductions.WebRtc
{
    public class GlobalOptions
    {
        public bool UseSignalingThread = true;
        public bool UseWorkerThread = true;
        public bool ForceSoftwareVideoEncoder = false;
        public bool AutoShutdown = true;
        public TraceLevel MinimumLogLevel = TraceLevel.Warning;
        public bool LogToStandardError = true;
        public bool LogToDebugOutput = false;

        public bool IsSingleThreaded
        {
            set => UseSignalingThread = UseWorkerThread = value;
        }
    }
}