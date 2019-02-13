using System;
using System.Diagnostics;

namespace WonderMediaProductions.WebRtc
{
    public static class TraceLevelExtensions
    {
        public static ConsoleColor ToConsoleColor(this TraceLevel level)
        {
            switch (level)
            {
                case TraceLevel.Error:
                    return ConsoleColor.Red;
                case TraceLevel.Info:
                    return ConsoleColor.DarkCyan;
                case TraceLevel.Verbose:
                    return ConsoleColor.DarkGreen;
                case TraceLevel.Warning:
                    return ConsoleColor.Yellow;
                default:
                    return ConsoleColor.DarkCyan;
            }
        }

        public static void WriteToConsole(this TraceLevel level, string line)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ToConsoleColor(level);
            Console.WriteLine($"[WebRTC {level:G}]:\t{line}");
            Console.ForegroundColor = color;
        }
    }
}