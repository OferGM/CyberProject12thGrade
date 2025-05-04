//PngEncoder.cs
using System.Drawing.Imaging;
using System.Drawing;

namespace CredentialsExtractor.Core
{
    public class PngEncoder : IImageEncoder
    {
        public string FileExtension => ".png";

        public void SaveImage(Stream stream, Bitmap image)
        {
            image.Save(stream, ImageFormat.Png);
        }
    }
}
