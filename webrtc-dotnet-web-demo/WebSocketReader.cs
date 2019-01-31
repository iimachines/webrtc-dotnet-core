using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WonderMediaProductions.WebRtc
{
    public sealed class WebSocketReader
    {
        private byte[] _data;
        private int _size;
        private readonly CancellationToken _cancellation;
        private readonly int _blockSize;

        public WebSocketReader(WebSocket socket, CancellationToken cancellation, int blockSize = 4096)
        {
            Socket = socket;
            _cancellation = cancellation;
            _blockSize = blockSize;
            _data = new byte[blockSize];
            _size = 0;
        }

        public WebSocket Socket { get; }

        public bool CanRead => !Socket.CloseStatus.HasValue;

        private Task<WebSocketReceiveResult> ReceiveAsync()
        {
            // Make room for one more block if needed
            if (_size + _blockSize > _data.Length)
            {
                Array.Resize(ref _data, _size + _blockSize);
            }

            lock (Socket)
            {
                return Socket.ReceiveAsync(new ArraySegment<byte>(_data, _size, _data.Length - _size), _cancellation);
            }
        }

        /// <summary>
        /// Reads a full message. Returns null when the socket got closed.
        /// </summary>
        public async Task<ArraySegment<byte>> ReadBytesAsync()
        {
            WebSocketReceiveResult result;

            _size = 0;

            do
            {
                result = await ReceiveAsync();
                if (result.CloseStatus.HasValue)
                    return null;
                _size += result.Count;
            } while (!result.EndOfMessage);

            return new ArraySegment<byte>(_data, 0, _size);
        }

        public async Task<JObject> ReadJsonAsync()
        {
            var segment = await ReadBytesAsync();
            if (segment.Array == null)
                return null;

            var message = Encoding.UTF8.GetString(segment.Array, 0, segment.Count);
            return JObject.Parse(message);
        }
    }
}