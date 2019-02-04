using System.Collections.Generic;
using SharpDX;

namespace WonderMediaProductions.WebRtc
{
    public sealed class ComObjectComparer : IEqualityComparer<ComObject>
    {
        public bool Equals(ComObject x, ComObject y)
        {
            return x.NativePointer == y.NativePointer;
        }

        public int GetHashCode(ComObject obj)
        {
            return obj.NativePointer.GetHashCode();
        }

        public static readonly ComObjectComparer Instance = new ComObjectComparer();
    }
}