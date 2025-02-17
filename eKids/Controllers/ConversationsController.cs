using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Database.Shared.Enums;
using eKids.Hubs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly IRepository<Conversations> _conversationsRepository;
        private readonly ILogger<ConversationsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationsHub> _notificationsHub;

        public ConversationsController(IRepository<Conversations> conversationsRepository, IHubContext<NotificationsHub> notificationsHub, ApplicationDbContext context, ILogger<ConversationsController> logger)
        {
            _conversationsRepository = conversationsRepository;
            _logger = logger;
            _context = context;
            _notificationsHub = notificationsHub;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageDto messageDto)
        {
            var message = new Conversations
            {
                SenderUsername = messageDto.SenderUsername,
                ReceiverUsername = messageDto.ReceiverUsername,
                Content = messageDto.Content,
                IsRead = messageDto.IsRead,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            

            _conversationsRepository.Add(message);
            await _conversationsRepository.SaveAsync(default);
            return Ok(message);
        }

        [HttpPost("/api/Conversations/ShareToUser")]
        public async Task<IActionResult> ShareToUser([FromQuery] ShareType shareType, [FromBody] ShareItemDto shareDto)
        {
            try
            {
                var getReceiver = await _context.Users.FirstOrDefaultAsync(c => c.Username == shareDto.ReceiverUsername);
                if(getReceiver == null)
                {
                    return BadRequest(new { Message = "Invalid username" });
                }

                if(string.IsNullOrEmpty(shareDto.SenderUsername) || string.IsNullOrEmpty(shareDto.ReceiverUsername))
                {
                    return BadRequest(new { Message = "Missing data" });
                } 

                switch (shareType)
                {
                    case ShareType.Quiz:
                        await HandleQuizShare(shareDto);
                        break;
                    case ShareType.Lesson:
                        await HandleLessonShare(shareDto);
                        break;
                    case ShareType.Course:
                        await HandleCourseShare(shareDto);
                        break;
                    case ShareType.Blogs:
                        await HandleBlogShare(shareDto);
                        break;
                    default: return BadRequest(new { Message = "invalid share type" });
                }

                var connectedUser = ConnectionMapping.GetConnectionId(getReceiver.Username);
                if(connectedUser != null)
                {
                    var responseTitle =
                        shareType == ShareType.Blogs ? $"{shareDto.SenderUsername} të dërgoi një blog"
                        : shareType == ShareType.Lesson ? $"{shareDto.SenderUsername} të dërgoi nje leksion"
                        : shareType == ShareType.Course ? $"{shareDto.SenderUsername} të dërgoi nje kurs"
                        : $"{shareDto.SenderUsername} të dërgoi nje kuiz";

                    var responseId =
                        shareType == ShareType.Blogs ? shareDto.BlogId : shareType == ShareType.Course ? shareDto.CourseId : shareType == ShareType.Lesson ? shareDto.LessonId : shareDto.QuizId;
                    var response = new
                    {
                        toastTitle = responseTitle,
                        toastShareType = shareType,
                        toastShareId = responseId
                    };
                    await _notificationsHub.Clients.Client(connectedUser).SendAsync("ShareToaster", response);
                }

                return Ok(new { Message = "Item shared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in sharing to user${shareDto.ReceiverUsername}");
                return BadRequest(new { Message = "Error in sharing user" });
            }
        }

        private async Task HandleBlogShare(ShareItemDto shareItem)
        {
            var blogCheck = await _context.Blogs.FindAsync(shareItem.BlogId);
            if(blogCheck == null)
            {
                throw new ApplicationException("Inavlid blog id provided");
            }

            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                BlogId = shareItem.BlogId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
        }

        private async Task HandleLessonShare(ShareItemDto shareItem)
        {
            var lessonCheck = await _context.Lessons.FindAsync(shareItem.LessonId);
            if(lessonCheck == null)
            {
                throw new ArgumentException("Invalid lesson id provided");
            }
            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                LessonId = shareItem.LessonId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
        }
        private async Task HandleCourseShare(ShareItemDto shareItem)
        {
            var courseCheck = await _context.Courses.FindAsync(shareItem.CourseId);
            if(courseCheck == null)
            {
                throw new ArgumentException("Invalid course id provided");
            }
            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                CourseId = shareItem.CourseId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
        }

        private async Task HandleQuizShare(ShareItemDto shareItem)
        {
            var quizCheck = await _context.Quizzes.FindAsync(shareItem.QuizId);
            if(quizCheck == null)
            {
                throw new ArgumentException("Invalid quizid provided");
            }
            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                QuizId = shareItem.QuizId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };
            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
        }

        [HttpGet("/api/Conversations/{sender}/{receiver}")]
        public async Task<IActionResult> GetMessagesMade(string sender, string receiver, [FromQuery] int page = 1, [FromQuery] int pageSize = 15, CancellationToken token = default)
        {
            try
            {
                if (page <= 0)
                {
                    return BadRequest("Page must be greater than 0.");
                }

                if (pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest("Page size must be between 1 and 100.");
                }

                var skip = (page - 1) * pageSize;

                var messages = await _context.Conversations
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(c => (c.SenderUsername == sender && c.ReceiverUsername == receiver) || (c.SenderUsername == receiver && c.ReceiverUsername == sender))
                    .Include(c => c.Quiz)      // Include Quiz if it's related to Conversations
                    .Include(c => c.Lesson)    // Include Lesson if it's related to Conversations
                    .Include(c => c.Course)
                    .Select(c => new
                    {
                        c.ID,
                        c.Content,
                        c.FileUrl,
                        c.IsRead,
                        c.SenderUsername,
                        c.ReceiverUsername,
                        c.CreatedAt,
                        Sender = new
                        {
                            c.Sender.Firstname,
                            c.Sender.Lastname,
                            c.Sender.Username,
                            c.Sender.ProfilePictureUrl,
                        },
                        Receiver = new
                        {
                            c.Receiver.Firstname,
                            c.Receiver.Lastname,
                            c.Receiver.Username,
                            c.Receiver.ProfilePictureUrl
                        },
                        c.Quiz,
                        c.Lesson,
                        c.Course
                    })
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync(token);


                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving messages for {sender} and reciver {receiver}");
                return BadRequest(new { Message = "Error retriving messages" });
            }
        }

        [HttpPatch("/api/Conversations/ReadMessages/{sender}/{receiver}")]
        public async Task<IActionResult> ReadMessages(string sender, string receiver, CancellationToken token)
        {
            try
            {
                var messages = await _context.Conversations
                    .Where(c => (c.SenderUsername == sender && c.ReceiverUsername == receiver) || (c.SenderUsername == receiver && c.ReceiverUsername == sender))
                    .Where(r => (r.ReceiverUsername == receiver || r.SenderUsername == receiver))
                    .ExecuteUpdateAsync(setter => setter.SetProperty(m => m.IsRead, true));

                return Ok(new { Message = "Messages read suscessfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error making read messages for users: {sender} and {receiver}");
                return BadRequest(new { Message = "Error reading messages" });
            }
        }

    }
}
