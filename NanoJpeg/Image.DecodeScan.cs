using System;

namespace NanoJpeg
{
    public partial class Image
    {
        private void DecodeScan(ref ImageData data, DecodeData decodeData)
        {
            int mbx, mby, sbx, sby;
            int rstcount = data.RestartInterval, nextrst = 0;
            var channels = data.Channels;

            int length = DecodeLength(ref data);
            if (length < (4 + 2 * channels.Length)) { throw new DecodeException(ErrorCode.SyntaxError); }
            if (data[0] != channels.Length) { throw new DecodeException(ErrorCode.Unsupported); }

            data.Skip(1);
            length -= 1;

            for (int i = 0; i < channels.Length; i++)
            {
                if (data[0] != channels[i].Cid) { throw new DecodeException(ErrorCode.SyntaxError); }
                if ((data[1] & 0xEE) != 0) { throw new DecodeException(ErrorCode.SyntaxError); }

                channels[i].Dctabsel = data[1] >> 4;
                channels[i].Actabsel = (data[1] & 1) | 2;

                data.Skip(2);
                length -= 2;
            }

            if (data[0] != 0 || data[1] != 63 || data[2] != 0) { throw new DecodeException(ErrorCode.Unsupported); }

            data.Skip(length);
            mbx = mby = 0;

            while (true)
            {
                for (int i = 0; i < channels.Length; i++)
                {
                    for (sby = 0; sby < channels[i].Ssy; ++sby)
                    {
                        for (sbx = 0; sbx < channels[i].Ssx; ++sbx)
                        {
                            DecodeBlock(
                                ref data,
                                decodeData,
                                channels[i],
                                ((mby * channels[i].Ssy + sby) * channels[i].Stride + mbx * channels[i].Ssx + sbx) << 3);
                        }
                    }
                }

                if (++mbx >= data.MbWidth)
                {
                    mbx = 0;
                    if (++mby >= data.MbHeight) { break; }
                }

                if (data.RestartInterval != 0 && --rstcount == 0)
                {
                    data.BufBits &= 0xF8;
                    int i = GetBits(ref data, 16);
                    if ((i & 0xFFF8) != 0xFFD0 || (i & 7) != nextrst) { throw new DecodeException(ErrorCode.SyntaxError); }

                    nextrst = (nextrst + 1) & 7;
                    rstcount = data.RestartInterval;
                    for (i = 0; i < 3; ++i) { channels[i].Dcpred = 0; }
                }
            }
        }

        private void DecodeBlock(ref ImageData data, DecodeData decodeData, ChannelData channel, int pixelIndex)
        {
            byte code = 0;
            int coef = 0;

            decodeData.ClearBlock();

            var outv = channel.Pixels.AsSpan(pixelIndex);
            var huffman = decodeData.HuffmanTables;
            byte[][] quantization = decodeData.QuantizationTables;
            int[] block = decodeData.Block;

            channel.Dcpred += GetVlc(ref data, huffman[channel.Dctabsel], ref code);
            block[0] = channel.Dcpred * quantization[channel.Qtsel][0];

            do
            {
                int value = GetVlc(ref data, huffman[channel.Actabsel], ref code);

                if (code == 0) { break; } // EOB
                if ((code & 0x0F) == 0 && code != 0xF0) { throw new DecodeException(ErrorCode.SyntaxError); }

                coef += (code >> 4) + 1;
                if (coef > 63) { throw new DecodeException(ErrorCode.SyntaxError); }

                block[njZZ[coef]] = value * quantization[channel.Qtsel][coef];
            } while (coef < 63);

            for (int i = 0; i < 64; i += 8) { RowIdct(block.AsSpan(i)); }
            for (int i = 0; i < 8; ++i) { ColumnIdct(block.AsSpan(i), outv.Slice(i), channel.Stride); }
        }

        private int GetVlc(ref ImageData data, VlcCode[] vlc, ref byte code)
        {
            int value = ShowBits(ref data, 16);
            int bits = vlc[value].Bits;
            if (bits == 0) { throw new DecodeException(ErrorCode.SyntaxError); }

            SkipBits(ref data, bits);
            value = vlc[value].Code;

            code = (byte)value;
            bits = value & 15;
            if (bits == 0) { return 0; }

            value = GetBits(ref data, bits);
            if (value < (1 << (bits - 1))) { value += ((-1) << bits) + 1; }

            return value;
        }

        private void SkipBits(ref ImageData data, int bits)
        {
            if (data.BufBits < bits) { ShowBits(ref data, bits); }
            data.BufBits -= bits;
        }

        private int GetBits(ref ImageData data, int bits)
        {
            int res = ShowBits(ref data, bits);
            SkipBits(ref data, bits);
            return res;
        }

        private int ShowBits(ref ImageData data, int bits)
        {
            byte newbyte;
            if (bits == 0) { return 0; }

            while (data.BufBits < bits)
            {
                if (data.Remaining <= 0)
                {
                    data.Buf = (data.Buf << 8) | 0xFF;
                    data.BufBits += 8;
                    continue;
                }

                newbyte = data.Advance();
                data.Remaining--;
                data.BufBits += 8;
                data.Buf = (data.Buf << 8) | newbyte;

                if (newbyte == 0xFF)
                {
                    if (data.Remaining != 0)
                    {
                        byte marker = data.Advance();
                        data.Remaining--;
                        switch (marker)
                        {
                            case 0x00:
                            case 0xFF:
                                break;

                            case 0xD9:
                                data.Remaining = 0;
                                break;

                            default:
                                if ((marker & 0xF8) != 0xD0) { throw new DecodeException(ErrorCode.SyntaxError); }
                                else
                                {
                                    data.Buf = (data.Buf << 8) | marker;
                                    data.BufBits += 8;
                                }
                                break;
                        }
                    }
                    else { throw new DecodeException(ErrorCode.SyntaxError); }
                }
            }

            return (data.Buf >> (data.BufBits - bits)) & ((1 << bits) - 1);
        }

        private void RowIdct(Span<int> blk)
        {
            int x0, x1, x2, x3, x4, x5, x6, x7, x8;
            if (((x1 = blk[4] << 11)
                | (x2 = blk[6])
                | (x3 = blk[2])
                | (x4 = blk[1])
                | (x5 = blk[7])
                | (x6 = blk[5])
                | (x7 = blk[3])) == 0)
            {
                blk[0] = blk[1] = blk[2] = blk[3] = blk[4] = blk[5] = blk[6] = blk[7] = blk[0] << 3;
                return;
            }

            x0 = (blk[0] << 11) + 128;
            x8 = W7 * (x4 + x5);
            x4 = x8 + (W1 - W7) * x4;
            x5 = x8 - (W1 + W7) * x5;
            x8 = W3 * (x6 + x7);
            x6 = x8 - (W3 - W5) * x6;
            x7 = x8 - (W3 + W5) * x7;
            x8 = x0 + x1;
            x0 -= x1;
            x1 = W6 * (x3 + x2);
            x2 = x1 - (W2 + W6) * x2;
            x3 = x1 + (W2 - W6) * x3;
            x1 = x4 + x6;
            x4 -= x6;
            x6 = x5 + x7;
            x5 -= x7;
            x7 = x8 + x3;
            x8 -= x3;
            x3 = x0 + x2;
            x0 -= x2;
            x2 = (181 * (x4 + x5) + 128) >> 8;
            x4 = (181 * (x4 - x5) + 128) >> 8;

            blk[0] = (x7 + x1) >> 8;
            blk[1] = (x3 + x2) >> 8;
            blk[2] = (x0 + x4) >> 8;
            blk[3] = (x8 + x6) >> 8;
            blk[4] = (x8 - x6) >> 8;
            blk[5] = (x0 - x4) >> 8;
            blk[6] = (x3 - x2) >> 8;
            blk[7] = (x7 - x1) >> 8;
        }

        private void ColumnIdct(Span<int> blk, Span<byte> outv, int stride)
        {
            int x0, x1, x2, x3, x4, x5, x6, x7, x8, position = 0;
            if (((x1 = blk[8 * 4] << 8)
                | (x2 = blk[8 * 6])
                | (x3 = blk[8 * 2])
                | (x4 = blk[8 * 1])
                | (x5 = blk[8 * 7])
                | (x6 = blk[8 * 5])
                | (x7 = blk[8 * 3])) == 0)
            {
                x1 = Clip(((blk[0] + 32) >> 6) + 128);
                for (x0 = 8; x0 != 0; --x0)
                {
                    outv[position] = (byte)x1;
                    position += stride;
                }

                return;
            }

            x0 = (blk[0] << 8) + 8192;
            x8 = W7 * (x4 + x5) + 4;
            x4 = (x8 + (W1 - W7) * x4) >> 3;
            x5 = (x8 - (W1 + W7) * x5) >> 3;
            x8 = W3 * (x6 + x7) + 4;
            x6 = (x8 - (W3 - W5) * x6) >> 3;
            x7 = (x8 - (W3 + W5) * x7) >> 3;
            x8 = x0 + x1;
            x0 -= x1;
            x1 = W6 * (x3 + x2) + 4;
            x2 = (x1 - (W2 + W6) * x2) >> 3;
            x3 = (x1 + (W2 - W6) * x3) >> 3;
            x1 = x4 + x6;
            x4 -= x6;
            x6 = x5 + x7;
            x5 -= x7;
            x7 = x8 + x3;
            x8 -= x3;
            x3 = x0 + x2;
            x0 -= x2;
            x2 = (181 * (x4 + x5) + 128) >> 8;
            x4 = (181 * (x4 - x5) + 128) >> 8;

            outv[position] = Clip(((x7 + x1) >> 14) + 128); position += stride;
            outv[position] = Clip(((x3 + x2) >> 14) + 128); position += stride;
            outv[position] = Clip(((x0 + x4) >> 14) + 128); position += stride;
            outv[position] = Clip(((x8 + x6) >> 14) + 128); position += stride;
            outv[position] = Clip(((x8 - x6) >> 14) + 128); position += stride;
            outv[position] = Clip(((x0 - x4) >> 14) + 128); position += stride;
            outv[position] = Clip(((x3 - x2) >> 14) + 128); position += stride;
            outv[position] = Clip(((x7 - x1) >> 14) + 128);
        }
    }
}
