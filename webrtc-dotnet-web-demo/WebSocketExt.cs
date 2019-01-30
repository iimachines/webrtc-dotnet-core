using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace webrtc_dotnet_demo
{
    public static class WebSocketExt
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static Task SendDataAsync(this WebSocket socket, ArraySegment<byte> message, CancellationToken cancellation = default)
        {
            lock (socket)
            {
                return socket.SendAsync(message, WebSocketMessageType.Binary, true, cancellation);
            }
        }

        public static Task SendTextAsync(this WebSocket socket, string message, CancellationToken cancellation = default)
        {
            var data = Encoding.UTF8.GetBytes(message);
            lock (socket)
            {
                return socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cancellation);
            }
        }

        public static Task SendJsonAsync(this WebSocket socket, string action, object payload, CancellationToken cancellation = default)
        {
            var message = new {action, payload};
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings));
            lock (socket)
            {
                return socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cancellation);
            }
        }
    }
}
