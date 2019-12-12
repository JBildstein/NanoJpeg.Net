using System;
using System.Runtime.CompilerServices;

namespace NanoJpeg
{
    internal ref struct ImageData
    {
        public byte this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Data[Position + index]; }
        }

        public readonly Span<byte> Data;
        public int Position;
        public int Remaining;

        public int RestartInterval;
        public ChannelData[] Channels;
        public int MbWidth;
        public int MbHeight;
        public int BufBits;
        public int Buf;

        public ImageData(byte[] data)
            : this()
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }

            Data = data.AsSpan();
            Remaining = data.Length;
        }

        public ImageData(Span<byte> data)
            : this()
        {
            Data = data;
            Remaining = data.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> Slice(int offset, int length)
        {
            return Data.Slice(Position + offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int count)
        {
            Position += count;
            Remaining -= count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Advance()
        {
            return Data[Position++];
        }
    }
}
