using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SharpDX;

namespace WonderMediaProductions.WebRtc
{
    public sealed class ComReflection
    {
        public static IEnumerable<ComObject> GetComObjectFields(object self)
        {
            if (self == null)
                return Enumerable.Empty<ComObject>();

            return self.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(f => typeof(ComObject).IsAssignableFrom(f.FieldType) &&
                            f.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                .Select(f => f.GetValue(self))
                .OfType<ComObject>()
                .Distinct(ComObjectComparer.Instance);
        }
    }
}