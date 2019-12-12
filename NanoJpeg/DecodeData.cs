using System;

namespace NanoJpeg
{
    internal sealed class DecodeData
    {
        public VlcCode[][] HuffmanTables { get; }
        public byte[][] QuantizationTables { get; }
        public int[] Block { get; }

        public DecodeData()
        {
            HuffmanTables = new VlcCode[4][] { new VlcCode[65536], new VlcCode[65536], new VlcCode[65536], new VlcCode[65536] };
            QuantizationTables = new byte[4][] { new byte[64], new byte[64], new byte[64], new byte[64] };
            Block = new int[64];
        }

        public void ClearBlock()
        {
            Array.Clear(Block, 0, Block.Length);
        }
    }
}
