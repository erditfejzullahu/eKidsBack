using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class VideoFileService : IVideoFileService
    {
        private readonly string[] _allowedExtensions = { ".mp4", ".mov", ".avi", ".mkv" };
        private readonly string[] _allowedMimeTypes = { "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska" };
        private const long _maxFileSize = 1024 * 1024 * 1024; // 1024 MB

        public bool isValidFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mimeType = file.ContentType;

            return file.Length <= _maxFileSize &&
                _allowedExtensions.Contains(extension) && _allowedMimeTypes.Contains(mimeType);
        }

        public string SanitizeFileName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_").Replace("..", "") + Path.GetExtension(fileName);
        }
    }
}
