using System;
using System.Collections.Generic;

namespace WonderMediaProductions.WebRtc
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Lifts a sequence of disposable items into a disposable list of items
        /// </summary>
        public static DisposableList<T> ToDisposableList<T>(this IEnumerable<T> items) 
            where T : IDisposable
        {
            return new DisposableList<T>(items);
        }
    }
}