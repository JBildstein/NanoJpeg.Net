using System;

namespace NanoJpeg
{
    public partial class Image
    {
        private void DecodeStartOfFrame(ref ImageData data)
        {
            int length = DecodeLength(ref data);
            int startPosition = data.Position;

            if (length < 9) { throw new DecodeException(ErrorCode.SyntaxError); }
            if (data[0] != 8) { throw new DecodeException(ErrorCode.Unsupported); }

            int height = Height = Decode16(ref data, 1);
            int width = Width = Decode16(ref data, 3);

            if (width == 0 || height == 0) { throw new DecodeException(ErrorCode.SyntaxError); }

            int channelCount = data[5];
            data.Skip(6);

            if (channelCount != 1 && channelCount != 3) { throw new DecodeException(ErrorCode.Unsupported); }

            if (length < (channelCount * 3)) { throw new DecodeException(ErrorCode.SyntaxError); }

            int ssxmax = 0, ssymax = 0;
            var channels = new ChannelData[channelCount];

            for (int i = 0; i < channelCount; i++)
            {
                channels[i] = new ChannelData();
                channels[i].Cid = data[0];
                if ((channels[i].Ssx = data[1] >> 4) == 0) { throw new DecodeException(ErrorCode.SyntaxError); }
                if ((channels[i].Ssx & (channels[i].Ssx - 1)) != 0) { throw new DecodeException(ErrorCode.Unsupported); } // non-power of two
                if ((channels[i].Ssy = data[1] & 15) == 0) { throw new DecodeException(ErrorCode.SyntaxError); }
                if ((channels[i].Ssy & (channels[i].Ssy - 1)) != 0) { throw new DecodeException(ErrorCode.Unsupported); }  // non-power of two
                if (((channels[i].Qtsel = data[2]) & 0xFC) != 0) { throw new DecodeException(ErrorCode.SyntaxError); }

                data.Skip(3);
                if (channels[i].Ssx > ssxmax) { ssxmax = channels[i].Ssx; }
                if (channels[i].Ssy > ssymax) { ssymax = channels[i].Ssy; }
            }

            if (channelCount == 1)
            {
                channels[0].Ssx = channels[0].Ssy = ssxmax = ssymax = 1;
            }

            int mbsizex = ssxmax << 3;
            int mbsizey = ssymax << 3;
            int mbwidth = (width + mbsizex - 1) / mbsizex;
            int mbheight = (height + mbsizey - 1) / mbsizey;

            for (int i = 0; i < channelCount; i++)
            {
                channels[i].Width = (width * channels[i].Ssx + ssxmax - 1) / ssxmax;
                channels[i].Height = (height * channels[i].Ssy + ssymax - 1) / ssymax;
                channels[i].Stride = mbwidth * channels[i].Ssx << 3;

                if ((channels[i].Width < 3 && channels[i].Ssx != ssxmax) ||
                    (channels[i].Height < 3 && channels[i].Ssy != ssymax))
                {
                    throw new DecodeException(ErrorCode.Unsupported);
                }

                channels[i].Pixels = new byte[channels[i].Stride * mbheight * channels[i].Ssy << 3];
            }

            data.Skip(length - (data.Position - startPosition));

            data.Channels = channels;
            data.MbWidth = mbwidth;
            data.MbHeight = mbheight;
        }

        private void DecodeHuffmanTables(ref ImageData data, DecodeData decodeData)
        {
            Span<byte> counts = stackalloc byte[16];
            int length = DecodeLength(ref data);
            int remain, spread;

            while (length >= 17)
            {
                int i = data[0];
                if ((i & 0xEC) != 0) { throw new DecodeException(ErrorCode.SyntaxError); }
                if ((i & 0x02) != 0) { throw new DecodeException(ErrorCode.Unsupported); }

                i = (i | (i >> 3)) & 3;  // combined DC/AC + tableid value

                for (int codelen = 1; codelen <= 16; codelen++) { counts[codelen - 1] = data[codelen]; }

                data.Skip(17);
                length -= 17;

                var vlc = decodeData.HuffmanTables[i];
                int vlcidx = 0;
                remain = spread = 65536;
                for (int codelen = 1; codelen <= 16; codelen++)
                {
                    spread >>= 1;
                    int currcnt = counts[codelen - 1];

                    if (currcnt == 0) { continue; }
                    if (length < currcnt) { throw new DecodeException(ErrorCode.SyntaxError); }

                    remain -= currcnt << (16 - codelen);
                    if (remain < 0) { throw new DecodeException(ErrorCode.SyntaxError); }

                    for (int k = 0; k < currcnt; k++)
                    {
                        byte code = data[k];
                        for (int j = spread; j != 0; j--)
                        {
                            vlc[vlcidx].Bits = (byte)codelen;
                            vlc[vlcidx].Code = code;
                            vlcidx++;
                        }
                    }

                    data.Skip(currcnt);
                    length -= currcnt;
                }
            }

            if (length != 0) { throw new DecodeException(ErrorCode.SyntaxError); }
        }

        private void DecodeQuantizationTables(ref ImageData data, DecodeData decodeData)
        {
            int length = DecodeLength(ref data);

            while (length >= 65)
            {
                int i = data[0];
                if ((i & 0xFC) != 0) { throw new DecodeException(ErrorCode.SyntaxError); }

                byte[] t = decodeData.QuantizationTables[i];
                for (int j = 0; j < t.Length; j++) { t[j] = data[j + 1]; }

                data.Skip(65);
                length -= 65;
            }

            if (length != 0) { throw new DecodeException(ErrorCode.SyntaxError); }
        }

        private int DecodeRestartInterval(ref ImageData data)
        {
            int length = DecodeLength(ref data);
            if (length < 2) { throw new DecodeException(ErrorCode.SyntaxError); }
            int restartInterval = Decode16(ref data);
            data.Skip(length);

            return restartInterval;
        }
    }
}
