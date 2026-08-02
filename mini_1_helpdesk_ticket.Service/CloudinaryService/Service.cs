using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using mini_1_helpdesk_ticket.Service.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace mini_1_helpdesk_ticket.Service.CloudinaryService;

public class Service: IService   
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _cloudinaryOptions = new();

    public Service(IConfiguration configuration)
    {
        configuration.GetSection(nameof(CloudinaryOptions)).Bind(_cloudinaryOptions);
        _cloudinary = new Cloudinary(new Account(
                _cloudinaryOptions.CloudName,
                _cloudinaryOptions.ApiKey,
                _cloudinaryOptions.ApiSecret
            )
        );
    }
    
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null", nameof(file));
        }

        if (!IsImageFile(file))
        {
            throw new ArgumentException("File is not an image", nameof(file));
        }
        //đọc ảnh
        await using var stream = file.OpenReadStream();
        //upload ảnh lên server: tên và nôi dung (stream)
        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(file.FileName, stream),
        };
        //tiến hành upload
        var uploadResult = await _cloudinary.UploadAsync( uploadParams);
        //trả về string
        return uploadResult.SecureUrl.ToString();
    }
    //check đuôi file
    private bool IsImageFile(IFormFile file)
    {
        //This is a basic check. For more robust validation, consider using a libarary like MimeDetective
        var allowsExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        return allowsExtensions.Contains(fileExtension);
    }
}