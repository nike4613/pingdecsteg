using System;
using System.Runtime.Serialization;

namespace pngsteg.PNG_Decoder
{
    [Serializable]
    internal class PngDecoderException : Exception
    {
        public PngDecoderException()
        {
        }

        public PngDecoderException(string message) : base(message)
        {
        }

        public PngDecoderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PngDecoderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}