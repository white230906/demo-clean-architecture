using Microsoft.AspNetCore.Http;

namespace mini_1_helpdesk_ticket.Service.MediaService;

public interface IService
{
    public Task<string> UploadImageAsync(IFormFile file);
}