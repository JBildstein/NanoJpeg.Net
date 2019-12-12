using System;

namespace NanoJpeg
{
    /// <summary>
    /// Exception for decoding errors
    /// </summary>
    public class DecodeException : Exception
    {
        /// <summary>
        /// The error code of this exception
        /// </summary>
        public ErrorCode ErrorCode { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="DecodeException"/> class
        /// </summary>
        /// <param name="ErrorCode">The error code of this exception</param>
        public DecodeException(ErrorCode ErrorCode)
            : base(ErrorCode.ToString())
        {
            this.ErrorCode = ErrorCode;
        }
    }
}