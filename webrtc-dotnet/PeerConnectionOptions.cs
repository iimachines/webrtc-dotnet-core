using System.Collections.Generic;

namespace WonderMediaProductions.WebRtc
{
    public class PeerConnectionOptions
    {
        public List<string> TurnServers  = new List<string>();
        public List<string> StunServers  = new List<string>();
        public bool CanReceiveAudio;
        public bool CanReceiveVideo;
        public string UserName;
        public string PassWord;
        public bool IsDtlsSrtpEnabled  = true;
    }
}