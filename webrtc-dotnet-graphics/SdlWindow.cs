using System;
using System.Diagnostics;
using System.Threading;
using Vanara.PInvoke;
using static SDL2.SDL;

namespace WonderMediaProductions.WebRtc.GraphicsD3D11
{
    internal class SdlWindow : Disposable
    {
        private IntPtr _nativePtr;
        private static int _initCounter;

        static SdlWindow()
        {
            // Allow native DLLs to be found in our assembly directory
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            Kernel32.SetDllDirectory(directory);
        }

        public SdlWindow(string title, int width, int height)
        {
            if (Interlocked.Increment(ref _initCounter) == 1)
            {
                SDL_Init(0);
                SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
            }

            _nativePtr = SDL_CreateWindow(title,
                SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
                width, height,
                SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI);
        }

        public IntPtr NativeHandle
        {
            get
            {
                var windowInfo = new SDL_SysWMinfo();
                SDL_GetWindowWMInfo(_nativePtr, ref windowInfo);
                return windowInfo.info.win.window;
            }
        }

        public void PollAllPendingEvents()
        {
            while (SDL_PollEvent(out var ev) > 0)
            {
            }
        }

        protected override void OnDispose(bool isDisposing)
        {
            SDL_DestroyWindow(_nativePtr);
            _nativePtr = IntPtr.Zero;

            Debug.Assert(_initCounter > 0);

            if (Interlocked.Decrement(ref _initCounter) == 0)
            {
                SDL_Quit();
            }
        }
    }
}
