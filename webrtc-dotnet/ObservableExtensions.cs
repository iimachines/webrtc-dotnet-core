using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace WonderMediaProductions.WebRtc
{
    public static class ObservableExtensions
    {
        /// <summary>
        /// Swallows all <see cref="ObjectDisposedException"/> object thrown.
        /// </summary>
        /// <remarks>
        /// It is possible an observer is being disposed on thread A while being invoked on thread B.
        /// We could puts locks everywhere, but that would defeat the async nature of RX.
        /// </remarks>
        public static bool TryOnNext<T>(this IObserver<T> observer, in T value)
        {
            try
            {
                observer.OnNext(value);
                return true;
            }
            catch(ObjectDisposedException)
            {
                return false;
            }
        }
    }
}