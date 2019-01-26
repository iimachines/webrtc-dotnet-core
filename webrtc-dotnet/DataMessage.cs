namespace webrtc_dotnet_standard
{
    public sealed class DataMessage
    {
        public string Label { get; }
        public string Content { get; }

        public DataMessage(string label, string content)
        {
            Label = label;
            Content = content;
        }

        public override string ToString()
        {
            return $"{nameof(Label)}: {Label}, {nameof(Content)}: {Content}";
        }
    }
}