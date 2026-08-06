namespace WMS.Application.Interfaces;

public interface IImageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string publicId, int width, int height);
}