namespace webrtc_dotnet_standard
{
    public sealed class SessionDescription
    {
        public SessionDescription(string type, string content)
        {
            Type = type;
            Content = content;
        }

        public string Type { get; }
        public string Content { get; }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(Content)}: {Content}";
        }
    }
}