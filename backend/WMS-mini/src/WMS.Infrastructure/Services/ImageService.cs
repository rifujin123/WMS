using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

public class ImageService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public ImageService(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string publicId, int width, int height)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            PublicId = publicId,
            Overwrite = true,
            Transformation = new Transformation().Width(width).Height(height).Crop("fill")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        if (uploadResult.Error != null)
            throw new Exception(uploadResult.Error.Message);

        return uploadResult.SecureUrl.ToString();
    }
}