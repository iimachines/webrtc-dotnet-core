using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using webrtc_dotnet_standard;

namespace webrtc_dotnet_demo
{
    public static class RtcServer
    {
        public static async Task Run(WebSocket ws)
        {
            var receiveBuffer = new byte[1024 * 4];

            using (var pc = new ObservablePeerConnection("server", options => { }))
            using (pc.LocalIceCandidateStream.Subscribe(ice => ws.SendJsonAsync("ice", ice)))
            using (pc.LocalSessionDescriptionStream.Subscribe(sd => ws.SendJsonAsync("sdp", sd)))
            {
                var msgStream = Observable.Never<DataMessage>();
                var iceStream = new Subject<IceCandidate>();
                var sdpStream = new Subject<SessionDescription>();

                pc.Connect(msgStream, sdpStream, iceStream);

                pc.AddDataChannel("data", DataChannelFlag.None);
                pc.AddStream(StreamTrack.Video);

                pc.CreateOffer();

                while (!ws.CloseStatus.HasValue)
                {
                    var message = await ws.ReceiveJsonAsync(default, receiveBuffer);
                    if (message == null)
                        break;

                    switch (message["action"].Value<string>())
                    {
                        case "ice":
                        {
                            iceStream.OnNext(new IceCandidate(message["payload"]));
                            break;
                        }

                        case "sdp":
                        {
                            sdpStream.OnNext(new SessionDescription(message["payload"]));
                            break;
                        }
                    }
                }
            }
        }
    }
}