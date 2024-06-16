using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IRepository<MediaLibrary> _mediaRepository;
        private readonly IWebHostEnvironment _environment;


        public FileController(IRepository<MediaLibrary> mediaRepository, IWebHostEnvironment environment)
        {
            _mediaRepository = mediaRepository;
            _environment = environment;

        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadFile([FromBody] UploadFile uploadFile)
        {
            if (string.IsNullOrEmpty(uploadFile.FileBase64))
            {
                return BadRequest("Missing base64data");
            }

            var dataUriPattern = new Regex(@"^data:(?<mimeType>[\w/\-]+)(;charset=[\w\-]+)?(;base64)?,(?<data>.*)$");


            var match = dataUriPattern.Match(uploadFile.FileBase64);
            if (!match.Success)
            {
                return BadRequest("Invalid base64 data.");
            }

            var mimeType = match.Groups["mimeType"].Value;
            var base64Data = match.Groups["data"].Value;

            var mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "image/jpeg", "jpg" },
                { "image/png", "png" },
                { "image/gif", "gif" },
                { "image/bmp", "bmp" },
                { "image/webp", "webp" }
            };

            if (!mimeTypeMappings.TryGetValue(mimeType, out string extension))
            {
                return BadRequest("Invalid file extension.");
            }

            string fileName = $"{Guid.NewGuid()}.{extension}";

            byte[] bytes = Convert.FromBase64String(base64Data);

            var uploadsFolderPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            string filePath = Path.Combine(uploadsFolderPath, fileName);
            System.IO.File.WriteAllBytes(filePath, bytes);

            var url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            var theFile = new MediaLibrary
            {
                FileName = uploadFile.FileName,
                FileUrl = url,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _mediaRepository.Add(theFile);
            await _mediaRepository.SaveAsync(default);

            return Ok(new { theFile });

        }

        [HttpGet("/api/allMedia")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllMedia()
        {
            var media = await _mediaRepository.GetAll().ToListAsync();
            if(media == null)
            {
                return BadRequest();
            }

            return Ok(media);   
        }

        [HttpDelete("/api/deleteMedia/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMedia(int id, CancellationToken token)
        {
            try
            {
                var media = await _mediaRepository.Get(id, token);
                if (media == null)
                {
                    return NotFound(new { message = "Media not found" });
                }

                await _mediaRepository.Delete(media.ID, token);
                await _mediaRepository.SaveAsync(token);

                return Ok(new { message = "Media deleted successfully", media });
            }
            catch (Exception ex)
            {
                // Log the exception
                //_logger.LogError(ex, "Error deleting media with ID {MediaID}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the media" });
            }
        }
    }
}
