using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using eKids.Hubs;
using Humanizer;
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
    public class QuizCompletationController : ControllerBase
    {
        private readonly IRepository<QuizzesCompleted> _quizCompletationRep;
        private readonly ILogger<QuizCompletationController> _logger;
        private readonly IHubContext<NotificationsHub> _notificationsHub;
        private readonly ApplicationDbContext _context;

        public QuizCompletationController(ApplicationDbContext context, IRepository<QuizzesCompleted> quizCompletationRep, IHubContext<NotificationsHub> notificationsHub, ILogger<QuizCompletationController> logger)
        {
            _quizCompletationRep = quizCompletationRep;
            _logger = logger;
            _notificationsHub = notificationsHub;
            _context = context;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateQuizCompleteStarted(QuizCompStartDto quizCompDto, CancellationToken token)
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

                if(quizCompDto == null)
                {
                    return BadRequest("Data missing");
                }

                var exists = await _quizCompletationRep.IsExist(c => c.QuizId == quizCompDto.QuizId && c.UserId == userId, token);

                if (exists)
                {
                    return Conflict(new { Message = "Quiz already started" });
                }

                var quizCompleted = new QuizzesCompleted
                {
                    UserId = userId,
                    QuizId = quizCompDto.QuizId,
                    Completed = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };

                await _context.QuizzesCompleted.AddAsync(quizCompleted, token);

                CultureInfo albanianCulture = new CultureInfo("sq-AL");
                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi fillimin e kuizit {quizCompleted.Quiz.QuizName} me {DateTime.Now.ToString("f", albanianCulture)}",
                    Type = Shared.Enums.NotificationsType.ProgressTrackingNotification,
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

                return Ok(new { Message = "Quiz started" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in starting Quiz");
                return BadRequest(new { Message = "Error in starting quiz" });
            }
        }

        [Authorize]
        [HttpPatch]
        public async Task<IActionResult> UpdateQuizCompletationStatus(QuizCompletationDto quizComp, CancellationToken token)
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
                var quiz = await _context.QuizzesCompleted.Where(c => c.QuizId == quizComp.QuizId && c.UserId == userId).FirstOrDefaultAsync(token);

                if(quiz == null)
                {
                    return NotFound(new { Message = "Quiz not found" });
                }
                if(quiz.UserId != userId)
                {
                    return Forbid();
                }

                quiz.Completed = quizComp.Completed;
                //quiz.Mistakes = quizComp.Mistakes;
                //quiz.Duration = quizComp.Duration;
                quiz.LastModified = DateTime.UtcNow;
                _context.QuizzesCompleted.Update(quiz);

                CultureInfo albanianCulture = new CultureInfo("sq-AL");
                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi perfundimin e kuizit {quiz.Quiz.QuizName} me {DateTime.Now.ToString("f", albanianCulture)}",
                    Type = Shared.Enums.NotificationsType.CompletedProgressNotification,
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
                return Ok(new { Message = "Successfully status updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updaing quiz completation status with id {quizComp.QuizId}");
                return BadRequest(new {Message="Error updating quiz completation status"});
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuizCompletedById(int id, CancellationToken token)
        {
            try
            {
                var quiz = await _quizCompletationRep.Get(id, token, c => c.Quiz);
                if(quiz == null)
                {
                    return NotFound(new { Message = "No quiz found" });
                }
                return Ok(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving quiz with id {id}");
                return BadRequest("Error retriving quiz");
            }
        }

        [HttpGet("/api/QuizzesCompleted/GetByUser/{userId}")]
        public async Task<IActionResult> GetQuizCompeltedByUserId(int userId, CancellationToken token)
        {
            try
            {
                var quiz = await _quizCompletationRep.GetAll().AsNoTracking().Include(c => c.Quiz).AsNoTracking().Where(c => c.UserId == userId && c.Completed == true).ToListAsync(token);
                if(quiz.Count == 0)
                {
                    return NotFound(new { Message = "No quiz found" });
                }
                return Ok(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retriving quiz with user id {userId}");
                return BadRequest("Error retriving quiz");
            }
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteQuizCompletation(int id, CancellationToken token)
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

                var quiz = await _context.QuizzesCompleted.Where(c => c.ID == id).FirstOrDefaultAsync(token);
                if(quiz == null)
                {
                    return NotFound(new { Message = "Not found" });
                }
                if(quiz.UserId != userId)
                {
                    return Forbid();
                }

                _context.QuizzesCompleted.Remove(quiz);

                CultureInfo albanianCulture = new CultureInfo("sq-AL");
                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi perfundimin e kuizit {quiz.Quiz.QuizName} me {DateTime.Now.ToString("f", albanianCulture)}",
                    Type = Shared.Enums.NotificationsType.CompletedProgressNotification,
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
                return Ok(new { Message = "Quiz deleted" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in deleting quiz compeltation status");
                return BadRequest(new { Message = "Error deleting quiz completation status" });
            }
        }

        [Authorize]
        [HttpPatch("/api/QuizzesCompleted/UpdateQuizMistakes/")]
        public async Task<IActionResult> UpdateMistakes(QuizCompStartDto updateMistakesDto, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var quiz = await _quizCompletationRep.GetAll().AsNoTracking().FirstOrDefaultAsync(c => c.QuizId == updateMistakesDto.QuizId && c.UserId == userId, token);
                if(quiz == null)
                {
                    return NotFound(new { Message = "Not found quiz" });
                }
                if(quiz.UserId != userId)
                {
                    return Forbid();
                }
                quiz.Mistakes += 1;
                _quizCompletationRep.Update(quiz);
                await _quizCompletationRep.SaveAsync(token);
                return Ok(new { Message = "Mistake updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mistake");
                return BadRequest(new { Message = "Error updating mistake" });
            }
        }

        [Authorize]
        [HttpGet("/api/QuizzesCompletation/GetStatusQuizz/{quizId}")]
        public async Task<IActionResult> GetStatusOfQuiz(int quizId, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                if (userId <= 0 || quizId <= 0)
                {
                    return BadRequest(new { Message = "Invalid userId or quizId" });
                }

                var quizStatus = await _quizCompletationRep.GetAll().AsNoTracking().FirstOrDefaultAsync(c => c.QuizId == quizId && c.UserId == userId, token);
                if(quizStatus == null)
                {
                    return NotFound(new { Message = "No quiz found" });
                }
                return Ok(quizStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in getting status of quiz");
                return BadRequest(new { Message = "Error in getting status of quiz" });
            }
        }



    }
}
