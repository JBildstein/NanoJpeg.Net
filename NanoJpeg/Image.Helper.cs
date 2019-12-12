using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace NanoJpeg
{
    public partial class Image
    {
        private static void SkipMarker(ref ImageData data)
        {
            int length = DecodeLength(ref data);
            data.Skip(length);
        }

        private static int DecodeLength(ref ImageData data)
        {
            if (data.Remaining < 2) { throw new DecodeException(ErrorCode.SyntaxError); }
            int length = Decode16(ref data);

            if (length > data.Remaining) { throw new DecodeException(ErrorCode.SyntaxError); }
            data.Skip(2);

            return length - 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Decode16(ref ImageData data)
        {
            return Decode16(ref data, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Decode16(ref ImageData data, int offset)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Clip(int x)
        {
            if ((x & (~0xFF)) != 0) { return (byte)((-x) >> 31); }
            else { return (byte)x; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte CF(int x)
        {
            x = (x + 64) >> 7;
            return Clip(x);
        }
    }
}
