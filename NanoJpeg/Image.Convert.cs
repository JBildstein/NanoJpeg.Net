using System;
using System.Numerics;

namespace NanoJpeg
{
    public partial class Image
    {
        private static readonly Vector3 VecCb = new Vector3(-88, 0, 454);
        private static readonly Vector3 VecCr = new Vector3(-183, 359, 0);

        private void ConvertYcc(ref ImageData data, bool flip)
        {
            int w = Width;
            int h = Height;

            var channels = data.Channels;
            for (int i = 0; i < channels.Length; i++)
            {
                while (channels[i].Width < w || channels[i].Height < h)
                {
                    if (channels[i].Width < w) { UpsampleH(channels[i]); }
                    if (channels[i].Height < h) { UpsampleV(channels[i]); }
                }

                if (channels[i].Width < w || channels[i].Height < h) { throw new DecodeException(ErrorCode.InternalError); }
            }

            if (channels.Length == 3)
            {
                Data = new byte[w * h * 3];

                var prgb = Data.AsSpan();
                var py = channels[0].Pixels.AsSpan();
                var pcb = channels[1].Pixels.AsSpan();
                var pcr = channels[2].Pixels.AsSpan();

                int rs = channels[0].Stride - w;
                int gs = channels[1].Stride - w;
                int bs = channels[2].Stride - w;

                int rgbidx = 0;
                int yidx, cbidx, cridx;
                yidx = cbidx = cridx = 0;

                for (int yy = h; yy != 0; --yy)
                {
                    for (int x = 0; x < w; ++x)
                    {
                        int y = py[yidx++] << 8;
                        int cb = pcb[cbidx++] - 128;
                        int cr = pcr[cridx++] - 128;

                        int g = (y - 88 * cb - 183 * cr + 128) >> 8;
                        int r = (y + 359 * cr + 128) >> 8;
                        int b = (y + 454 * cb + 128) >> 8;

                        if (flip)
                        {
                            prgb[rgbidx++] = Clip(r);
                            prgb[rgbidx++] = Clip(g);
                            prgb[rgbidx++] = Clip(b);
                        }
                        else
                        {
                            prgb[rgbidx++] = Clip(b);
                            prgb[rgbidx++] = Clip(g);
                            prgb[rgbidx++] = Clip(r);
                        }
                    }

                    yidx += rs;
                    cbidx += gs;
                    cridx += bs;
                }
            }
            else if (channels[0].Width != channels[0].Stride)
            {
                var channel = channels[0];

                // grayscale -> only remove stride
                int d = channel.Stride - channel.Width;
                if (d == 0) { Data = channel.Pixels; }
                else
                {
                    Data = new byte[w * h];
                    for (int y = 0; y < channel.Height; y++)
                    {
                        Buffer.BlockCopy(
                            channel.Pixels,
                            y * channel.Stride,
                            Data,
                            y * channel.Width,
                            channel.Width);
                    }
                }
            }
        }

        private void UpsampleH(ChannelData c)
        {
            int xmax = c.Width - 3;
            byte[] outdata = new byte[(c.Width * c.Height) << 1];
            var lin = c.Pixels.AsSpan();
            var lout = outdata.AsSpan();

            int linidx = 0;
            int loutidx = 0;

            for (int y = c.Height; y != 0; --y)
            {
                var iv1 = new Vector3(lin[linidx], lin[linidx + 1], lin[linidx + 2]);
                lout[loutidx] = CF((int)Vector3.Dot(iv1 * CF2AB, Vector3.One));
                lout[loutidx + 1] = CF((int)Vector3.Dot(iv1 * CF3XYZ, Vector3.One));
                lout[loutidx + 2] = CF((int)Vector3.Dot(iv1 * CF3ABC, Vector3.One));

                for (int x = 0; x < xmax; ++x)
                {
                    int tin = linidx + x;
                    int tout = loutidx + (x << 1);
                    var iv2 = new Vector4(lin[tin], lin[tin + 1], lin[tin + 2], lin[tin + 3]);
                    lout[tout + 3] = CF((int)Vector4.Dot(iv2 * CF4ABCD, Vector4.One));
                    lout[tout + 4] = CF((int)Vector4.Dot(iv2 * CF4DCBA, Vector4.One));
                }

                linidx += c.Stride;
                loutidx += c.Width << 1;

                var iv3 = new Vector3(lin[linidx - 1], lin[linidx - 2], lin[linidx - 3]);
                lout[loutidx - 1] = CF((int)Vector3.Dot(iv3 * CF3ABC, Vector3.One));
                lout[loutidx - 2] = CF((int)Vector3.Dot(iv3 * CF3XYZ, Vector3.One));
                lout[loutidx - 3] = CF((int)Vector3.Dot(iv3 * CF2AB, Vector3.One));
            }

            c.Width <<= 1;
            c.Stride = c.Width;
            c.Pixels = outdata;
        }

        private void UpsampleV(ChannelData c)
        {
            int w = c.Width, s1 = c.Stride, s2 = s1 + s1;
            byte[] outdata = new byte[(c.Width * c.Height) << 1];
            var cout = outdata.AsSpan();
            var cin = c.Pixels.AsSpan();

            for (int x = 0; x < w; ++x)
            {
                int outidx = x;
                int inidx = x;

                var iv1 = new Vector3(cin[inidx], cin[inidx + s1], cin[inidx + s2]);
                cout[outidx] = CF((int)Vector3.Dot(iv1 * CF2AB, Vector3.One)); outidx += w;
                cout[outidx] = CF((int)Vector3.Dot(iv1 * CF3XYZ, Vector3.One)); outidx += w;
                cout[outidx] = CF((int)Vector3.Dot(iv1 * CF3ABC, Vector3.One)); outidx += w;
                inidx += s1;

                for (int y = c.Height - 3; y != 0; --y)
                {
                    var iv2 = new Vector4(cin[inidx - s1], cin[inidx], cin[inidx + s1], cin[inidx + s2]);
                    cout[outidx] = CF((int)Vector4.Dot(iv2 * CF4ABCD, Vector4.One)); outidx += w;
                    cout[outidx] = CF((int)Vector4.Dot(iv2 * CF4DCBA, Vector4.One)); outidx += w;
                    inidx += s1;
                }

                inidx += s1;
                var iv3 = new Vector3(cin[inidx], cin[inidx - s1], cin[inidx - s2]);
                cout[outidx] = CF((int)Vector3.Dot(iv3 * CF3ABC, Vector3.One)); outidx += w;
                cout[outidx] = CF((int)Vector3.Dot(iv3 * CF3XYZ, Vector3.One)); outidx += w;
                cout[outidx] = CF((int)Vector3.Dot(iv3 * CF2AB, Vector3.One));
            }

            c.Height <<= 1;
            c.Stride = c.Width;
            c.Pixels = outdata;
        }
    }
}
