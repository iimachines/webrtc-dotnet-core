using System;

namespace webrtc_dotnet_standard
{
    public static class Configuration
    {
        public static T Options<T>(this Action<T> configure) where T: new()
        {
            var options = new T();
            configure(options);
            return options;
        }
    }
}