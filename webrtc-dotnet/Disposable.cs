using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WonderMediaProductions.WebRtc
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

        /// <summary>
        /// Dispose all disposable fields except some
        /// </summary>
        /// <remarks>
        /// The fields are disposed in reverse order of declaration
        /// </remarks>
        protected void DisposeAllFieldsExcept(params string[] excludeFieldNames)
        {
            var disposableFields = GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(f => !excludeFieldNames.Contains(f.Name) &&
                            typeof(IDisposable).IsAssignableFrom(f.FieldType) &&
                            f.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                .ToArray();

            for (var index = disposableFields.Length; --index  >= 0; )
            {
                var field = disposableFields[index];

                // Disposing one field can clear another field, so check this
                if (field.GetValue(this) is IDisposable disposable)
                {
                    var type = disposable.GetType();
                    Debug.WriteLine($"Disposing {type.Namespace}.{type.Name}...");
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Dispose all disposable fields
        /// </summary>
        /// <remarks>
        /// The fields are disposed in reverse order of declaration
        /// </remarks>
        protected void DisposeAllFields()
        {
            DisposeAllFieldsExcept();
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