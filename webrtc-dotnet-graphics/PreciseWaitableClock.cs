// ReSharper disable InconsistentNaming

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace WonderMediaProductions.WebRtc.GraphicsD3D11
{
    public class PreciseWaitableClock : Disposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("Kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeWaitHandle CreateWaitableTimerEx(IntPtr lpTimerAttributes, IntPtr lpTimerName, uint dwFlags, uint dwDesiredAccess);

        [DllImport("Kernel32", SetLastError = false, ExactSpelling = true)]
        private static extern void GetSystemTimePreciseAsFileTime(out FILETIME lpSystemTimeAsFileTime);

        [DllImport("Kernel32", SetLastError = false)]
        private static extern bool QueryPerformanceFrequency(out long lpPerformanceFreq);

        [DllImport("Kernel32", SetLastError = true)]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimer(
            [In] SafeWaitHandle hTimer, in FILETIME pDueTime, int lPeriod, [In] IntPtr pfnCompletionRoutine,
            [In] IntPtr lpArgToCompletionRoutine, [MarshalAs(UnmanagedType.Bool)] bool fResume);


        [DllImport("Kernel32", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CancelWaitableTimer([In] SafeWaitHandle hTimer);

        private static readonly double performanceTicks;
        private static readonly DateTime performanceCounterStart;
        private static readonly Func<DateTime> currentTimeFunc;

        static PreciseWaitableClock()
        {
            bool Windows8OrLater = Environment.OSVersion.Version >= new Version(6, 2);
            if (Windows8OrLater)
            {
                currentTimeFunc = GetCurrentTimeWin8;
            }
            else
            {
                if (!QueryPerformanceFrequency(out var performanceFrequency))
                {
                    throw new Win32Exception();
                }
                QueryPerformanceCounter(out var lpPerformanceCount);
                performanceTicks = (TimeSpan.TicksPerSecond / (double)performanceFrequency);
                long ticks = (long)(lpPerformanceCount * performanceTicks);
                performanceCounterStart = DateTime.Now.AddTicks(-ticks);
                currentTimeFunc = GetCurrentTimeWin7;
            }
        }

        public PreciseWaitableClock(EventResetMode eventResetMode)
        {
            const uint TIMER_ALL_ACCESS = 0x1F0003;
            const uint CREATE_WAITABLE_TIMER_MANUAL_RESET = 1;

            uint timerFlags = eventResetMode.HasFlag(EventResetMode.ManualReset) ? CREATE_WAITABLE_TIMER_MANUAL_RESET : 0;
            var handle = CreateWaitableTimerEx(IntPtr.Zero, IntPtr.Zero, timerFlags, TIMER_ALL_ACCESS);
            if (handle == null || handle.IsInvalid)
                throw new Win32Exception("CreateWaitableTimerEx failed");

            WaitHandle = new EventWaitHandle(false, eventResetMode)
            {
                SafeWaitHandle = handle
            };
        }

        public EventWaitHandle WaitHandle { get; }

        public DateTime GetCurrentTime() => currentTimeFunc();

        private static DateTime GetCurrentTimeWin8()
        {
            GetSystemTimePreciseAsFileTime(out var fileTime);
            long ticks = (((long)fileTime.dwHighDateTime) << 32) | fileTime.dwLowDateTime;
            return DateTime.FromFileTimeUtc(ticks);
        }

        private static DateTime GetCurrentTimeWin7()
        {
            unchecked
            {
                QueryPerformanceCounter(out var lpPerformanceCount);
                long ticks = (long)(lpPerformanceCount * performanceTicks);
                return performanceCounterStart.AddTicks(ticks);
            }
        }

        /// <summary>
        /// Sets the timer's wait-handle to fire once at some point in the future.
        /// </summary>
        /// <param name="eventTime"></param>
        public void SetFutureEventTime(DateTime eventTime)
        {
            unchecked
            {
                FILETIME fileTime;
                long ticks = eventTime.ToFileTimeUtc();
                fileTime.dwLowDateTime = (uint)(ticks & 0xFFFFFFFF);
                fileTime.dwHighDateTime = (uint)(ticks >> 32);
                if (!SetWaitableTimer(WaitHandle.SafeWaitHandle, fileTime, 0, IntPtr.Zero, IntPtr.Zero, false))
                    throw new Win32Exception("SetWaitableTimer failed");
            }
        }

        /// <summary>
        /// Cancels a previous call to <see cref="SetFutureEventTime"/>
        /// </summary>
        public void CancelFutureEventTime()
        {
            CancelWaitableTimer(WaitHandle.SafeWaitHandle);
        }

        protected override void OnDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                WaitHandle?.Dispose();
            }
        }
    }
}