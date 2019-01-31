using System;
using System.Collections.Generic;

namespace WonderMediaProductions.WebRtc
{
    /// <summary>
    /// When this list is disposed, it will dispose each item.
    /// </summary>
    public sealed class DisposableList<T> : List<T>, IDisposable where T : IDisposable
    {
        public DisposableList()
        {
        }

        public DisposableList(IEnumerable<T> collection) : base(collection)
        {
        }

        public void Dispose()
        {
            foreach (var obj in this)
            {
                obj?.Dispose();
            }

            Clear();
        }
    }
}