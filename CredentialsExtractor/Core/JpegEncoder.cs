// JpegEncoder.cs
using System.Drawing.Imaging;
using System.Drawing;

namespace CredentialsExtractor.Core
{
    public class JpegEncoder : IImageEncoder
    {
        private readonly long _quality;

        public JpegEncoder(long quality = 80)
        {
            _quality = quality;
        }

        public string FileExtension => ".jpeg";

        public void SaveImage(Stream stream, Bitmap image)
        {
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, _quality);

            ImageCodecInfo jpegEncoder = GetJpegEncoder();
            if (jpegEncoder != null)
            {
                image.Save(stream, jpegEncoder, encoderParams);
            }
            else
            {
                throw new InvalidOperationException("JPEG encoder not available");
            }
        }

        private ImageCodecInfo GetJpegEncoder()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    return codec;
                }
            }

            return null;
        }
    }
}