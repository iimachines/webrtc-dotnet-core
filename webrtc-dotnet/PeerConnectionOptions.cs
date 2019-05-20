using System;
using System.Collections.Generic;

namespace WonderMediaProductions.WebRtc
{
    public class PeerConnectionOptions
    {
        public string Name;
        public List<string> IceServers  = new List<string>();
        public string IceUsername; // TODO: Allow username/password per ICE server
        public string IcePassword;
        public bool CanReceiveAudio;
        public bool CanReceiveVideo;
        public bool IsDtlsSrtpEnabled  = true;
    }
}