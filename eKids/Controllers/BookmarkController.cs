using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookmarkController : ControllerBase
    {
        private readonly IRepository<Bookmarks> _bookmarksRepository;
        private readonly ILogger<BookmarkController> _logger;

        public BookmarkController(IRepository<Bookmarks> bookmarksRepository, ILogger<BookmarkController> logger)
        {
            _bookmarksRepository = bookmarksRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBookmark([FromBody] CreateBookmark bookmarkDto)
        {
            if(bookmarkDto == null)
            {
                return BadRequest(new { ErrorMessage = "No data provided" });
            }

            var bookmark = new Bookmarks
            {
                UserId = bookmarkDto.UserId,
                CourseId = bookmarkDto.CourseId,
                LessonId = bookmarkDto.LessonId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _bookmarksRepository.Add(bookmark);
            await _bookmarksRepository.SaveAsync(default);

            return Ok(bookmark);
        }

        [HttpGet("/api/Bookmarks/GetAll/{userId}")]
        public async Task<IActionResult> GetAllBookmarks(int userId, CancellationToken token)
        {
            var bookmarks = await _bookmarksRepository.GetAll().AsNoTracking().Where(c => c.UserId == userId).Include(c => c.Course).ThenInclude(c => c.Category).Include(c => c.Lesson).ThenInclude(c => c.Course).ThenInclude(c => c.Category).ToListAsync(token);
            if (bookmarks == null || !bookmarks.Any()) // Check for null or empty result
            {
                return BadRequest(new { Message = "No bookmarks founded" });
            }
            return Ok(bookmarks);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetBookmark(int userId, int? courseId, int? lessonId, CancellationToken token)
        {
            if (userId <= 0)
            {
                return BadRequest(new { Message = "Error in data " });
            }

            IQueryable<Bookmarks> bookmarks = _bookmarksRepository.GetAll().AsNoTracking().Where(c => c.UserId  == userId);


            if(courseId.HasValue && lessonId == null)
            {
                bookmarks = bookmarks.Where(c => c.CourseId == courseId).Include(c => c.Course);
            }
            else if(courseId == null && lessonId.HasValue)
            {
                bookmarks = bookmarks.Where(c => c.LessonId == lessonId).Include(c => c.Course);
            }
            else
            {
                return BadRequest(new { Message = "Error in data" });
            }

            var result = await bookmarks.ToListAsync(token);

            if (!result.Any())
            {
                return NotFound(new { Message = "No bookmarks found for the specified criteria." });
            }

            return Ok(result);
        }

        [HttpDelete("/api/Bookmark/DeleteById/{id}")]
        public async Task<IActionResult> DeleteBookmarkById(int id, CancellationToken token)
        {
            try
            {
                var bookmark = await _bookmarksRepository.Get(id, token);

                if(bookmark == null)
                {
                    return BadRequest(new {Message = "No bookmark found!"});
                }

                await _bookmarksRepository.Delete(bookmark.ID, token);
                await _bookmarksRepository.SaveAsync(token);

                return Ok(new { Message = "Bookmark Deleted!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting bookmark with id:{id}");
                return BadRequest(new { Message = "Error deleting bookmark!" });
            }
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteBookmark(int userId, int? courseId, int? lessonId, CancellationToken token)
        {
            IQueryable<Bookmarks> bookmarks = _bookmarksRepository.GetAll().Where(c => c.UserId == userId);

            if(courseId.HasValue && lessonId == null)
            {
                var bookmark = await bookmarks.FirstOrDefaultAsync(c => c.CourseId == courseId, token);
                if (bookmark == null)
                {
                    return NotFound(new { Message = "Not found bookmark via courseId" });
                }
                await _bookmarksRepository.Delete(bookmark.ID, token);

            }
            else if(courseId == null && lessonId.HasValue)
            {
                var bookmark = await bookmarks.FirstOrDefaultAsync(c => c.LessonId == lessonId, token);
                if(bookmark == null)
                {
                    return NotFound(new { Message = "Not found bookmark via courseId" });
                }
                await _bookmarksRepository.Delete(bookmark.ID, token);
            }
            else
            {
                return BadRequest(new { Message = "not found" });
            }
            await _bookmarksRepository.SaveAsync(token);
            return Ok(new { Message = "bookmark deleted" });

        }

    }
}
