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
using NuGet.Configuration;

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
                var getReceiver = await _context.Users.Where(c => c.Username == shareDto.ReceiverUsername).FirstOrDefaultAsync();
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
                    case ShareType.Discussion:
                        await HandleDiscussionShare(shareDto);
                        break;
                    case ShareType.Instructor:
                        await HandleInstructorShare(shareDto);
                        break;
                    case ShareType.InstructorCourse:
                        await HandleInstructorCourseShare(shareDto);
                        break;
                    case ShareType.InstructorLesson:
                        await HandleInstructorLessonShare(shareDto);
                        break;
                    case ShareType.InstructorOnlineMeeting:
                        await HandleInstructorOnlineMeeting(shareDto);
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
                        : shareType == ShareType.Quiz ? $"{shareDto.SenderUsername} të dërgoi nje kuiz"
                        : shareType == ShareType.Discussion ? $"{shareDto.SenderUsername} të dërgoi nje diskutim"
                        : shareType == ShareType.InstructorLesson ? $"{shareDto.SenderUsername} të dërgoi nje leksion online"
                        : shareType == ShareType.InstructorCourse ? $"{shareDto.SenderUsername} të dërgoi nje kurs online"
                        : shareType == ShareType.Instructor ? $"{shareDto.SenderUsername} të dërgoi nje instruktor"
                        : $"{shareDto.SenderUsername} të dërgoi nje takim online"
                        ;

                    var responseId =
                        shareType == ShareType.Blogs ? shareDto.BlogId : shareType == ShareType.Course ? shareDto.CourseId : shareType == ShareType.Lesson ? shareDto.LessonId : shareType == ShareType.Quiz ? shareDto.QuizId : shareType == ShareType.Discussion ? shareDto.DiscussionId : shareType == ShareType.Instructor ? shareDto.InstructorId : shareType == ShareType.InstructorCourse ? shareDto.InstructorCourseId : shareType == ShareType.InstructorLesson ? shareDto.InstructorLessonId : shareDto.OnlineMeetingId;
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
        private async Task HandleInstructorOnlineMeeting(ShareItemDto shareItem)
        {
            var onlineMeeting = await _context.OnlineMeetings.FindAsync(shareItem.OnlineMeetingId) ?? throw new ApplicationException("invalid online meeting id provided");
            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                OnlineMeetingId = shareItem.OnlineMeetingId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };
            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
        }
        private async Task HandleInstructorLessonShare(ShareItemDto shareItem)
        {
            var instructorLesson = await _context.InstructorLessons.FindAsync(shareItem.InstructorLessonId) ?? throw new ApplicationException("Invalid instructor lesson id provided");
            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                InstructorLessonId = shareItem.InstructorLessonId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
        }
        private async Task HandleInstructorCourseShare(ShareItemDto shareItem)
        {
            var instructorCourse = await _context.InstructorCourses.FindAsync(shareItem.InstructorCourseId) ?? throw new ApplicationException("Invalid instructor course id provided");
            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                InstructorCourseId = shareItem.InstructorCourseId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
        }
        private async Task HandleInstructorShare(ShareItemDto shareItem)
        {
            var instructor = await _context.Instructors.FindAsync(shareItem.InstructorId);
            if (instructor == null)
            {
                throw new ApplicationException("Invalid instructor id provided");
            }
            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                InstructorId = shareItem.InstructorId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
        }
        private async Task HandleDiscussionShare(ShareItemDto shareItem)
        {
            var discussionCheck = await _context.Discussions.FindAsync(shareItem.DiscussionId);
            if(discussionCheck == null)
            {
                throw new ApplicationException("Invalid discussion id provided");
            }

            var newConversation = new Conversations
            {
                SenderUsername = shareItem.SenderUsername,
                ReceiverUsername = shareItem.ReceiverUsername,
                DiscussionId = shareItem.DiscussionId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };
            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
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

            await _context.Conversations.AddAsync(newConversation);
            await _context.SaveChangesAsync();
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
        public async Task<IActionResult> GetMessagesMade(string sender, string receiver, [FromQuery] PaginationDto paginationDto , CancellationToken token = default)
        {
            try
            {
                paginationDto.Validate();

                var query = _context.Conversations.AsNoTracking().Where(c => (c.SenderUsername == sender && c.ReceiverUsername == receiver) || (c.SenderUsername == receiver && c.ReceiverUsername == sender));
                var totalCount = await query.CountAsync();

                var messages = await query
                    .AsSplitQuery()
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
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
                        Quiz = c.Quiz != null ? new
                        {
                            c.Quiz.ID,
                            c.Quiz.QuizName,
                            c.Quiz.QuizDescription,
                            c.Quiz.QuizCategory,
                            c.Quiz.CreatedAt
                        } : null,
                        Lesson = c.Lesson != null ? new
                        {
                            c.Lesson.ID,
                            c.Lesson.LessonName,
                            c.Lesson.LessonExcerpt,
                            c.Lesson.LessonFeaturedImage,
                            CourseCategory = c.Lesson.Course != null ? c.Lesson.Course.CourseCategory : 1,
                            c.Lesson.CreatedAt
                        } : null,
                        Blog = c.Blog != null ? new
                        {
                            c.Blog.ID,
                            c.Blog.Title,
                            c.Blog.Content,
                            c.Blog.CategoryId,
                            Username = c.Blog.User != null ? c.Blog.User.Username : null,
                            ProfilePictureUrl = c.Blog.User != null ? c.Blog.User.ProfilePictureUrl : null,
                            UserId = c.Blog.User != null ? c.Blog.User.ID : 0,
                            c.Blog.CreatedAt
                        } : null,
                        Course = c.Course != null ? new
                        {
                            c.Course.ID,
                            c.Course.CourseFeaturedImage,
                            c.Course.CourseName,
                            c.Course.CourseDescription,
                            c.Course.CourseCategory,
                            c.Course.CreatedAt
                        } : null,
                        InstructorCourse = c.InstructorCourse != null ? new
                        {
                            c.InstructorCourse.ID,
                            c.InstructorCourse.Name,
                            c.InstructorCourse.Description,
                            c.InstructorCourse.CategoryId,
                            c.InstructorCourse.Image,
                            c.InstructorCourse.CreatedAt
                        } : null,
                        InstructorLesson = c.InstructorLesson != null ? new
                        {
                            c.InstructorLesson.ID,
                            c.InstructorLesson.Title,
                            c.InstructorLesson.Content,
                            c.InstructorLesson.InstructorCourseSections.InstructorCourses.CategoryId,
                            c.InstructorLesson.InstructorCourseSections.InstructorCourses.Image,
                            c.InstructorLesson.CreatedAt,
                        } : null,
                        Instructor = c.Instructor != null ? new
                        {
                            c.Instructor.ID,
                            c.Instructor.UserId,
                            Name = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                            c.Instructor.User.ProfilePictureUrl,
                            InstructorCourses = c.Instructor.InstructorCourses.Count,
                            InstructorStudents = c.Instructor.InstructorStudents.Count,
                            c.CreatedAt
                        } : null,
                        OnlineMeeting = c.OnlineMeeting != null ? new
                        {
                            c.OnlineMeeting.ID,
                            c.OnlineMeeting.Title,
                            Course = c.OnlineMeeting.Course ?? null,
                            Lesson = c.OnlineMeeting.Lesson ?? null,
                            c.OnlineMeeting.Description,
                            c.OnlineMeeting.ViewCount,
                            c.OnlineMeeting.ScheduleDateTime,
                            DurationTime = c.OnlineMeeting.DurationTime ?? null,
                        } : null
                    })
                .ToListAsync(token);

                bool hasMore = (paginationDto.Skip + messages.Count) < totalCount;

                return Ok(new {messages, hasMore});
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
