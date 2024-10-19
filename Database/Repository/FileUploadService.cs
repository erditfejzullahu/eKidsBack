using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Database.Repository
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly IFileChecker _fileChecker;

        private readonly Dictionary<string, string> _mimeTypeMappings = new(StringComparer.InvariantCultureIgnoreCase)
        {

                { "image/jpeg", "jpg" },
                { "image/png", "png" },
                { "image/gif", "gif" },
                { "image/bmp", "bmp" },
                { "image/webp", "webp" },
                { "video/mp4", "mp4" },
                { "video/x-ms-wmv", "wmv" },
                { "video/x-msvideo", "avi" },
                { "video/mpeg", "mpeg" },
                { "video/quicktime", "mov" },
                { "video/webm", "webm" },
        };

        private readonly Dictionary<FileCategory, string> _folderMappings = new()
        {
            { FileCategory.Profile, "images/profiles" },
            { FileCategory.Other, "other" },
            { FileCategory.Uploads, "images/uploads" },
            { FileCategory.Nowhere, "nowhere"},
            { FileCategory.Videos, "videos"}
        };

        public FileUploadService(IHostEnvironment environment, ILogger<FileUploadService> logger, IFileChecker fileChecker)
        {
            _environment = environment;
            _logger = logger;
            _fileChecker = fileChecker;
        }   

        public async Task<string> UploadFile(string base64Data, FileCategory category)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(base64Data))
                {
                    throw new ArgumentException("Base64 data cannot be null or empty");
                }

                string mimeType = _fileChecker.ExtractMimeType(base64Data);
                if(string.IsNullOrWhiteSpace(mimeType))
                {
                    throw new ArgumentException("MIME type could not be determined from the base64 data.");
                }

                if(!_mimeTypeMappings.TryGetValue(mimeType, out string extension))
                {
                    throw new ArgumentException($"Unsupported MIME type: {mimeType}");
                }

                string base64String = _fileChecker.RemoveBase64Header(base64Data);
                byte[] fileBytes = Convert.FromBase64String(base64String);
                string filename = $"{Guid.NewGuid()}.{extension}";

                if(!_folderMappings.TryGetValue(category, out string folderPath))
                {
                    folderPath = _folderMappings[FileCategory.Nowhere];
                }

                string uploadsFolderPath;

                if (category == FileCategory.Profile)
                {
                    uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "wwwroot", folderPath);

                }
                else if (category == FileCategory.Uploads)
                {
                    uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "wwwroot", folderPath);

                }
                else if (category == FileCategory.Videos)
                {
                    uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "wwwroot", folderPath);
                }
                else if (category == FileCategory.Other)
                {
                    uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "wwwroot", folderPath);
                }
                else if(category == FileCategory.Nowhere)
                {
                    uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "wwwroot", folderPath);
                }
                else
                {
                    uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "wwwroot", folderPath);
                }

                if(!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }

                string filePath = Path.Combine(uploadsFolderPath, filename);
                await File.WriteAllBytesAsync(filePath, fileBytes);

                return $"/{folderPath}/{filename}";
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Base64 data is not in the correct format.");
                throw new InvalidOperationException("The provided data is not a valid base64 string.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading file.");
                throw new InvalidOperationException("An internal error occurred while uploading the file.", ex);
            }

        }
    }
}
