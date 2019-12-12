using System;

namespace NanoJpeg
{
    /// <summary>
    /// Represents a JPEG image.
    /// </summary>
    public partial class Image
    {
        /// <summary>
        /// Gets the width of the image.
        /// </summary>
        public int Width
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the height of the image.
        /// </summary>
        public int Height
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of color channels of this image.
        /// </summary>
        public int ChannelCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the raw pixel data of this image.
        /// </summary>
        public byte[] Data
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="data">The JPEG file data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
        /// <exception cref="DecodeException">The image could not be decoded.</exception>
        public Image(byte[] data)
        {
            Decode(data, false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="data">The JPEG file data.</param>
        /// <param name="flip">True to flip the red and blue channel (i.e. BGR order); False for normal RGB order.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
        /// <exception cref="DecodeException">The image could not be decoded.</exception>
        public Image(byte[] data, bool flip)
        {
            Decode(data, flip);
        }

        private void Decode(byte[] data, bool flip)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (data.Length < 2) { throw new DecodeException(ErrorCode.NoJpeg); }
            if (((data[0] ^ 0xFF) | (data[1] ^ 0xD8)) != 0) { throw new DecodeException(ErrorCode.NoJpeg); }

            var decodeData = new DecodeData();
            var imageData = new ImageData(data);

            imageData.Skip(2);

            bool reading = true;
            while (reading)
            {
                if ((imageData.Remaining < 2) || (imageData[0] != 0xFF)) { throw new DecodeException(ErrorCode.SyntaxError); }

                imageData.Skip(2);
                switch (imageData[-1])
                {
                    case 0xC0:
                        DecodeStartOfFrame(ref imageData);
                        break;

                    case 0xC4:
                        DecodeHuffmanTables(ref imageData, decodeData);
                        break;

                    case 0xDB:
                        DecodeQuantizationTables(ref imageData, decodeData);
                        break;

                    case 0xDD:
                        imageData.RestartInterval = DecodeRestartInterval(ref imageData);
                        break;

                    case 0xDA:
                        DecodeScan(ref imageData, decodeData);
                        reading = false;
                        break;

                    case 0xFE:
                        SkipMarker(ref imageData);
                        break;

                    default:
                        if ((imageData[-1] & 0xF0) == 0xE0) { SkipMarker(ref imageData); }
                        else { throw new DecodeException(ErrorCode.Unsupported); }
                        break;
                }
            }

            ChannelCount = imageData.Channels.Length;
            ConvertYcc(ref imageData, flip);
        }
    }
}
