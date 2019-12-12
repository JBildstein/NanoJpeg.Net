namespace NanoJpeg
{
    /// <summary>
    /// Error codes for decoding errors
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// Not a JPEG file
        /// </summary>
        NoJpeg,

        /// <summary>
        /// Unsupported format
        /// </summary>
        Unsupported,

        /// <summary>
        /// Internal error
        /// </summary>
        InternalError,

        /// <summary>
        /// Syntax error
        /// </summary>
        SyntaxError,
    }
}