using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using eKids.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookmarkController : ControllerBase
    {
        private readonly IRepository<Bookmarks> _bookmarksRepository;
        private readonly ILogger<BookmarkController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationsHub> _notificationsHub;
        public BookmarkController(IHubContext<NotificationsHub> notificationsHub, IRepository<Bookmarks> bookmarksRepository, ApplicationDbContext context, ILogger<BookmarkController> logger)
        {
            _bookmarksRepository = bookmarksRepository;
            _logger = logger;
            _context = context;
            _notificationsHub = notificationsHub;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBookmark([FromBody] CreateBookmark bookmarkDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new {Message = "Model not valid"});
                }
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue("Username");
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                if(bookmarkDto == null)
                {
                    return BadRequest(new { ErrorMessage = "No data provided" });
                }

                if (bookmarkDto.CourseId.HasValue)
                {
                    var courseExist = await _context.Courses.AsNoTracking().AnyAsync(c => c.ID == bookmarkDto.CourseId);
                    if (!courseExist)
                    {
                        return BadRequest(new { Message = "provide valid course id" });
                    }
                }

                if (bookmarkDto.LessonId.HasValue)
                {
                    var lessonExist = await _context.Lessons.AsNoTracking().AnyAsync(c => c.ID == bookmarkDto.LessonId);
                    if (!lessonExist)
                    {
                        return BadRequest(new { Message = "provide valid lesson id" });
                    }
                }
                if(bookmarkDto.LessonId.HasValue && bookmarkDto.CourseId.HasValue)
                {
                    return BadRequest(new { Message = "Provide only one Id" });
                }
                if (!bookmarkDto.LessonId.HasValue && !bookmarkDto.CourseId.HasValue)
                {
                    return BadRequest(new { Message = "It has to be at least one Id" });
                }

                var bookmark = new Bookmarks
                {
                    UserId = userId,
                    CourseId = bookmarkDto.CourseId,
                    LessonId = bookmarkDto.LessonId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Bookmarks.AddAsync(bookmark, token);

                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                var responseInformation = bookmark.CourseId.HasValue ? $"Njoftim mbi faqerimin e kursit {bookmark.Course?.CourseName}" : $"Njoftim mbi faqerimin e leksionit {bookmark.Lesson?.LessonName}";

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi krijimin e faqerimit {responseInformation}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification, token);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && c.IsRead == false).CountAsync();
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }

                return Ok(bookmark);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error creating bookmark");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("/api/Bookmarks/GetAll/")]
        public async Task<IActionResult> GetAllBookmarks( CancellationToken token)
        {
            var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
            {
                return Unauthorized();
            }

            var bookmarks = await _bookmarksRepository.GetAll().AsNoTracking().Where(c => c.UserId == userId).Include(c => c.Course).ThenInclude(c => c.Category).Include(c => c.Lesson).ThenInclude(c => c.Course).ThenInclude(c => c.Category).ToListAsync(token);
            if (bookmarks == null || !bookmarks.Any()) // Check for null or empty result
            {
                return BadRequest(new { Message = "No bookmarks founded" });
            }
            return Ok(bookmarks);
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize]
        [HttpDelete("/api/Bookmark/DeleteById/{id}")]
        public async Task<IActionResult> DeleteBookmarkById(int id, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue("Username");
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var bookmark = await _context.Bookmarks.FindAsync(id);

                if(bookmark == null)
                {
                    return BadRequest(new {Message = "No bookmark found!"});
                }

                if(bookmark.UserId != userId)
                {
                    return Forbid();
                }
                var responseInformation = bookmark.Course != null ? $"se kursit {bookmark.Course.CourseName}" : $"se leksionit {bookmark.Lesson?.LessonName}";
                _context.Bookmarks.Remove(bookmark);

                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi heqjen e faqerores {responseInformation}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                await _context.Notifications.AddAsync(notification, token);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && c.IsRead == false).CountAsync();
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }

                return Ok(new { Message = "Bookmark Deleted!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, $"Error in deleting bookmark with id:{id}");
                return BadRequest(new { Message = "Error deleting bookmark!" });
            }
        }

        [Authorize(Roles = "Admin")]
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
