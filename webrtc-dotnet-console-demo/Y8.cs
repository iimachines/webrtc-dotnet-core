using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.PixelFormats;

namespace WonderMediaProductions.WebRtc
{
    /// <summary>
    /// Packed pixel type containing a single 8 bit normalized luminance values.
    /// <para>
    /// Ranges from [0, 0, 0, 1] to [1, 1, 1, 1] in vector form.
    /// </para>
    /// </summary>
    public struct Y8 : IPixel<Y8>, IPackedVector<byte>
    {
        const int Sr = (299 * 0x10000 + 500) / 1000;
        const int Sg = (587 * 0x10000 + 500) / 1000;
        const int Sb = 0x10000 - Sr - Sg;

        public static float FromLinearRgb(float r, float g, float b)
        {
            return 0.299f * r + 0.587f * g + 0.114f * b;
        }

        public static byte FromLinearRgb(byte r, byte g, byte b)
        {
            unchecked
            {
                return (byte)((Sr * r + Sg * g + Sb * b) >> 16);
            }
        }

        public static ushort FromLinearRgb(ushort r, ushort g, ushort b)
        {
            unchecked
            {
                return (ushort)((Sr * r + Sg * g + Sb * b) >> 16);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Y8" /> struct.
        /// </summary>
        /// <param name="alpha">The alpha component</param>
        public Y8(float alpha)
        {
            PackedValue = Pack(alpha);
        }

        /// <inheritdoc />
        public byte PackedValue { get; set; }

        /// <summary>
        /// Compares two <see cref="Y8" /> objects for equality.
        /// </summary>
        /// <param name="left">
        /// The <see cref="Y8" /> on the left side of the operand.
        /// </param>
        /// <param name="right">
        /// The <see cref="Y8" /> on the right side of the operand.
        /// </param>
        /// <returns>
        /// True if the <paramref name="left" /> parameter is equal to the <paramref name="right" /> parameter; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Y8 left, Y8 right)
        {
            return left.PackedValue == right.PackedValue;
        }

        /// <summary>
        /// Compares two <see cref="Y8" /> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="Y8" /> on the left side of the operand.</param>
        /// <param name="right">The <see cref="Y8" /> on the right side of the operand.</param>
        /// <returns>
        /// True if the <paramref name="left" /> parameter is not equal to the <paramref name="right" /> parameter; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Y8 left, Y8 right)
        {
            return left.PackedValue != right.PackedValue;
        }

        /// <inheritdoc />
        public PixelOperations<Y8> CreatePixelOperations()
        {
            return new PixelOperations<Y8>();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PackFromScaledVector4(Vector4 vector)
        {
            PackFromVector4(vector);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 ToScaledVector4()
        {
            return ToVector4();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PackFromVector4(Vector4 vector)
        {
            PackedValue = Pack(FromLinearRgb(vector.X, vector.Y, vector.Z));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 ToVector4()
        {
            var g = PackedValue / (float)byte.MaxValue;
            return new Vector4(g, g, g, 1f);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PackFromRgba32(Rgba32 source)
        {
            PackedValue = FromLinearRgb(source.R, source.G, source.B);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PackFromArgb32(Argb32 source)
        {
            PackedValue = FromLinearRgb(source.R, source.G, source.B);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PackFromBgra32(Bgra32 source)
        {
            PackedValue = FromLinearRgb(source.R, source.G, source.B);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToRgb24(ref Rgb24 dest)
        {
            var y = PackedValue;
            dest.R = y;
            dest.G = y;
            dest.B = y;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToRgba32(ref Rgba32 dest)
        {
            var y = PackedValue;
            dest.R = y;
            dest.G = y;
            dest.B = y;
            dest.A = 255;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToArgb32(ref Argb32 dest)
        {
            var y = PackedValue;
            dest.R = y;
            dest.G = y;
            dest.B = y;
            dest.A = 255;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToBgr24(ref Bgr24 dest)
        {
            var y = PackedValue;
            dest.R = y;
            dest.G = y;
            dest.B = y;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToBgra32(ref Bgra32 dest)
        {
            var y = PackedValue;
            dest.R = y;
            dest.G = y;
            dest.B = y;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PackFromRgb48(Rgb48 source)
        {
            unchecked
            {
                PackedValue = (byte)(FromLinearRgb(source.R, source.G, source.B) * byte.MaxValue / ushort.MaxValue);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToRgb48(ref Rgb48 dest)
        {
            unchecked
            {
                var y = (ushort)(PackedValue * ushort.MaxValue / byte.MaxValue);
                dest.R = y;
                dest.G = y;
                dest.B = y;
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PackFromRgba64(Rgba64 source)
        {
            PackFromScaledVector4(source.ToScaledVector4());
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToRgba64(ref Rgba64 dest)
        {
            dest.PackFromScaledVector4(ToScaledVector4());
        }

        /// <summary>Compares an object with the packed vector.</summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the object is equal to the packed vector.</returns>
        public override bool Equals(object obj)
        {
            return obj is Y8 other && Equals(other);
        }

        /// <summary>
        /// Compares another Alpha8 packed vector with the packed vector.
        /// </summary>
        /// <param name="other">The Alpha8 packed vector to compare.</param>
        /// <returns>True if the packed vectors are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Y8 other)
        {
            return PackedValue == other.PackedValue;
        }

        /// <summary>Gets a string representation of the packed vector.</summary>
        /// <returns>A string representation of the packed vector.</returns>
        public override string ToString()
        {
            return (PackedValue / (float)byte.MaxValue).ToString();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return PackedValue.GetHashCode();
        }

        /// <summary>
        /// Packs a <see cref="T:System.Single" /> into a byte.
        /// </summary>
        /// <param name="y">The float containing the value to pack.</param>
        /// <returns>The <see cref="T:System.Byte" /> containing the packed values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Pack(float y)
        {
            unchecked
            {
                if (y <= 0)
                    return 0;

                if (y >= 1)
                    return byte.MaxValue;

                return (byte)(y * byte.MaxValue + 0.5f);
            }
        }
    }
}