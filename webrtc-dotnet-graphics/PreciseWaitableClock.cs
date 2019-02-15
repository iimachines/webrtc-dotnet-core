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

        [DllImport("Kernel32", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimer(
            [In] SafeWaitHandle hTimer, in FILETIME pDueTime, int lPeriod, [In] IntPtr pfnCompletionRoutine,
            [In] IntPtr lpArgToCompletionRoutine, [MarshalAs(UnmanagedType.Bool)] bool fResume);


        [DllImport("Kernel32", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CancelWaitableTimer([In] SafeWaitHandle hTimer);

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

        public DateTime GetCurrentTime()
        {
            unchecked
            {
                GetSystemTimePreciseAsFileTime(out var fileTime);
                long ticks = (((long)fileTime.dwHighDateTime) << 32) | fileTime.dwLowDateTime;
                return DateTime.FromFileTimeUtc(ticks);
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