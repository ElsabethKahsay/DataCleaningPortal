using Microsoft.AspNetCore.Http;

namespace ADDPerformance.Models
{
    // DTO used for multipart/form-data uploads so Swagger/Swashbuckle can generate a correct operation
    public class FileUploadDto
    {
        public string FileTypeChoice { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }
}