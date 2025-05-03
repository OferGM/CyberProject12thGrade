using System.Drawing;


namespace CredentialsExtractor.Core
{
    public interface IImageEncoder
    {
        void SaveImage(Stream stream, Bitmap image);
        string FileExtension { get; }
    }
}
