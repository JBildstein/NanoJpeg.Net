using System.Numerics;

namespace NanoJpeg
{
    public partial class Image
    {
        private const int W1 = 2841;
        private const int W2 = 2676;
        private const int W3 = 2408;
        private const int W5 = 1609;
        private const int W6 = 1108;
        private const int W7 = 565;

        private const int CF4A = -9;
        private const int CF4B = 111;
        private const int CF4C = 29;
        private const int CF4D = -3;
        private const int CF3A = 28;
        private const int CF3B = 109;
        private const int CF3C = -9;
        private const int CF3X = 104;
        private const int CF3Y = 27;
        private const int CF3Z = -3;
        private const int CF2A = 139;
        private const int CF2B = -11;

        private static readonly Vector3 CF2AB = new Vector3(CF2A, CF2B, 0);
        private static readonly Vector3 CF3XYZ = new Vector3(CF3X, CF3Y, CF3Z);
        private static readonly Vector3 CF3ABC = new Vector3(CF3A, CF3B, CF3C);
        private static readonly Vector4 CF4ABCD = new Vector4(CF4A, CF4B, CF4C, CF4D);
        private static readonly Vector4 CF4DCBA = new Vector4(CF4D, CF4C, CF4B, CF4A);

        private static readonly byte[] njZZ =
        {
            0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5, 12,
            19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28, 35,
            42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63
        };
    }
}
