using System.Drawing.Imaging;

namespace CredentialsExtractor.Core
{
    public class ImageEncoderFactory
    {
        public IImageEncoder CreateEncoder(ImageFormat format, long quality = 80)
        {
            if (format == ImageFormat.Jpeg)
            {
                return new JpegEncoder(quality);
            }

            if (format == ImageFormat.Png)
            {
                return new PngEncoder();
            }

            // Default to PNG if format not supported
            return new PngEncoder();
        }
    }
}
