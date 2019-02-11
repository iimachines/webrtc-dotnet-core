using System;
using System.Linq;

namespace WonderMediaProductions.WebRtc
{
    public sealed class DataMessage
    {
        public string Label { get; }
        public ArraySegment<byte> Content { get; }
        public MessageEncoding Encoding { get; }

        public string AsText => Content.Array == null
            ? null
            : System.Text.Encoding.UTF8.GetString(Content.Array, Content.Offset, Content.Count);

        public DataMessage(string label, ArraySegment<byte> content, MessageEncoding encoding = MessageEncoding.Binary)
        {
            Label = label;
            Content = content;
            Encoding = encoding;
        }

        public DataMessage(string label, byte[] data, MessageEncoding encoding = MessageEncoding.Binary)
        {
            Label = label;
            Content = new ArraySegment<byte>(data);
            Encoding = encoding;
        }

        public DataMessage(string label, string text, MessageEncoding encoding = MessageEncoding.Utf8)
        {
            Label = label;
            Content = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(text));
            Encoding = encoding;
        }

        public override string ToString()
        {
            var array = Content.Array;
            var offset = Content.Offset;

            if (array == null || Content.Count == 0)
            {
                return $"{Label} => empty message";
            }

            if (Encoding == MessageEncoding.Utf8)
            {
                var maxLength = Math.Min(100, Content.Count);
                var text = System.Text.Encoding.UTF8.GetString(array, offset, maxLength);
                var suffix = maxLength < Content.Count ? "..." : "";
                return $"{Label} => text message of length {Content.Count}: '{text}{suffix}'";
            }

            var header = string.Join(", ", array.Take(5).Select(b => b.ToString("X02")));
            return $"{Label} => binary message of length {Content.Count}: {header}...";
        }
    }
}