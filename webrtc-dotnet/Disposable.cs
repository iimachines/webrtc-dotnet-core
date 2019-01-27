using System;
using System.Diagnostics;

namespace webrtc_dotnet_standard
{
    public abstract class Disposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            GC.SuppressFinalize(this);
            OnDispose(true);
        }

        ~Disposable()
        {
            if (IsDisposed)
                return;

            Debug.WriteLine($"WARNING: {GetType().Name} was not disposed!");

            if (Debugger.IsAttached)
                Debugger.Break();

            OnDispose(false);
        }

        protected abstract void OnDispose(bool isDisposing);
    }
}