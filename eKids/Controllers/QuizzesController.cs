using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Transactions;
using System.Globalization;
using eKids.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizzesController : ControllerBase
    {
        private readonly IRepository<Quizzes> _quizzesRepository;
        private readonly IRepository<QuizQuestions> _quizQuestionsRep;
        private readonly IRepository<QuizAnswers> _quizAnswersRep;
        private readonly ILogger<QuizzesController> _logger;
        private readonly IRepository<Users> _userRepository;
        private readonly IRepository<QuizzesCompleted> _quizzesCompletedRepository;
        private readonly ISorterService<Quizzes> _sorterService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationsHub> _notificationsHub;

        public QuizzesController(IRepository<Quizzes> quizzesRepository,
            ILogger<QuizzesController> logger,
            IHubContext<NotificationsHub> notificationsHub,
            ApplicationDbContext context,
            IRepository<QuizQuestions> quizQuestionsRep,
            IRepository<QuizAnswers> quizAnswersRep,
            IRepository<Users> userRepository,
            IRepository<QuizzesCompleted> quizzesCompletedRepository,
            ISorterService<Quizzes> sorterService)
        {
            _notificationsHub = notificationsHub;
            _context = context;
            _logger = logger;
            _quizzesRepository = quizzesRepository;
            _quizQuestionsRep = quizQuestionsRep;
            _quizAnswersRep = quizAnswersRep;
            _userRepository = userRepository;
            _quizzesCompletedRepository = quizzesCompletedRepository;
            _sorterService = sorterService;
        }

        [Authorize]
        [HttpPost()]
        public async Task<IActionResult> CreateQuiz([FromBody] ProcessQuizDto quizDto, CancellationToken token)
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

                if (quizDto == null)
                {
                    return BadRequest("Quiz data is required");
                }

                // Validate string properties
                if (string.IsNullOrWhiteSpace(quizDto.QuizTitle))
                {
                    return BadRequest("Quiz title is required");
                }

                if (quizDto.QuizTitle.Length > 100) // Set reasonable limits
                {
                    return BadRequest("Quiz title cannot exceed 100 characters");
                }

                if (!string.IsNullOrEmpty(quizDto.QuizDescription) && quizDto.QuizDescription.Length > 500)
                {
                    return BadRequest("Quiz description cannot exceed 500 characters");
                }

                if (quizDto.QuizData == null || quizDto.QuizData.Count == 0)
                {
                    return BadRequest("Quiz questions data is required");
                }

                if (quizDto.QuizData.Count > 100)
                    return BadRequest("Cannot process more than 100 quiz items");

                var sanitizer = new HtmlSanitizer();

                var categories = await _context.Categories.AsNoTracking().Select(c => c.ID).ToListAsync();
                if (!categories.Contains(quizDto.QuizCategory))
                {
                    return BadRequest("Quiz category not supported");
                }

                var cleanQuiz = new ProcessQuizDto
                {
                    QuizTitle = sanitizer.Sanitize(quizDto.QuizTitle.Trim()),
                    QuizDescription = sanitizer.Sanitize(quizDto.QuizDescription.Trim()),
                    UserId = userId,
                    QuizCategory = quizDto.QuizCategory,
                };

                var quiz = new Quizzes
                {
                    QuizName = cleanQuiz.QuizTitle,
                    QuizDescription = cleanQuiz.QuizDescription,
                    UserId = cleanQuiz.UserId,
                    QuizCategory = cleanQuiz.QuizCategory,
                    ViewCount = 0,
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now
                };
                await _context.Quizzes.AddAsync(quiz, token);


                var questions = new List<QuizQuestions>();
                var answers = new List<QuizAnswers>();

                var questionMap = new Dictionary<int, QuizQuestions>();
                var answerMap = new Dictionary<int, QuizAnswers>();
                var answerIndexMap = new Dictionary<int, int>();

                foreach (var entry in quizDto.QuizData)
                {
                    var parts = entry.Key.Split("_");
                    //int parentId = int.Parse(parts[2]);

                    if (parts.Length < 4)
                    {
                        _logger.LogWarning($"Invalid key format: {entry.Key}");
                        continue;
                    }

                    if (!int.TryParse(parts[2], out int parentId) || parentId <= 0)
                    {
                        _logger.LogWarning($"Invalid parent ID in key: {entry.Key}");
                        continue;
                    }

                    if (entry.Value == null)
                    {
                        _logger.LogWarning($"Null value for key: {entry.Key}");
                        continue;
                    }


                    if (parts[3] == "question")
                    {
                        if (string.IsNullOrWhiteSpace(entry.Value.ToString()))
                        {
                            return BadRequest("No question text");
                        }

                        var sanitizedQuestion = sanitizer.Sanitize(entry.Value.ToString());

                        var question = new QuizQuestions
                        {
                            QuizId = quiz.ID,
                            QuestionText = sanitizedQuestion,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        };
                        questions.Add(question);
                        questionMap[parentId] = question;
                    }
                }
                await _context.QuizQuestions.AddRangeAsync(questions, token);

                foreach (var entry in quizDto.QuizData)
                {
                    var parts = entry.Key.Split("_");
                    //int parentId = int.Parse(parts[2]);

                    if (!int.TryParse(parts[2], out int parentId) || parentId <= 0)
                    {
                        _logger.LogWarning($"Invalid parent ID in key: {entry.Key}");
                        continue;
                    }

                    if (parts[3] == "answers" && questionMap.ContainsKey(parentId))
                    {
                        if (string.IsNullOrWhiteSpace(entry.Value.ToString()))
                        {
                            return BadRequest(new {Message = "answer text required"});
                        }

                        var sanitizedAnswer = sanitizer.Sanitize(entry.Value.ToString());
                        var answer = new QuizAnswers
                        {
                            AnswerText = sanitizedAnswer,
                            QuestionId = questionMap[parentId].ID,
                            IsCorrect = false,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        };
                        answers.Add(answer);
                        answerIndexMap[parentId] = answers.Count - 1;
                        answerMap[parentId] = answer;
                    }
                    else if (parts[3] == "correct" && answerMap.ContainsKey(parentId))
                    {
                        if(parts.Length > 3) {
                            var sanitizedCorrect = sanitizer.Sanitize(entry.Value.ToString().ToLower());
                            bool isCorrect = sanitizedCorrect == "true";
                            //int answerPosition = int.Parse(parts[4]); // The position/index of the correct answer (0-based)
                            if (!int.TryParse(parts[4], out int answerPosition))
                            {
                                _logger.LogWarning($"Invalid parent ID in key: {entry.Key}");
                                continue;
                            }
                            var getAnswers = answers.Where(c => c.QuestionId == questionMap[parentId].ID).ToList();
                            int index = 1;
                            foreach (var item in getAnswers)
                            {
                                if (index == answerPosition)
                                {
                                    item.IsCorrect = true;
                                }
                                index++;  // Increment the index after each iteration
                            }
                            }

                        //foreach (var ans in answers)
                        //{
                        //if (answerMap.ContainsKey(parentId))
                        //{
                        //    answers[answerPosition].IsCorrect = isCorrect;
                        //}
                        //}
                    }
                    else if (parts[3] == "type" && questionMap.ContainsKey(parentId))
                    {
                        //foreach( var que in questions)
                        //{
                        if (string.IsNullOrWhiteSpace(entry.Value.ToString()))
                        {
                            return BadRequest(new {Message = "Type required"});
                        }
                        var sanitizeQuestionType = sanitizer.Sanitize(entry.Value.ToString());
                        questionMap[parentId].QuestionType = sanitizeQuestionType;
                        //}
                    }

                }
                await _context.QuizAnswers.AddRangeAsync(answers, token);
                CultureInfo albanianCulture = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi krijimin e kursit {quiz.QuizName} me {DateTime.Now.ToString("f", albanianCulture)}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && c.IsRead == false).CountAsync();
                        await _notificationsHub.Clients.Client(username).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }

                return Ok(new {Message = "Quiz created successfully"});
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error while creating quiz");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while creating the quiz." });
            }
        }


        [HttpGet("/api/Quizzes/GetByUser/{userId}")]
        public async Task<IActionResult> GetAllQuizzesByUser([FromQuery] PaginationDto paginationDto, int userId, [FromQuery] SortQueryDto queryDto, [FromQuery] int? categoryId, CancellationToken token)
        {
            try
            {
                var query = _context.Quizzes.AsNoTracking();
                var allProgressQuizzes = await _quizzesCompletedRepository.GetAll().AsNoTracking().ToListAsync(token);

                if (categoryId.HasValue)
                {
                    query = query.Where(c => c.QuizCategory == categoryId.Value);
                }
                var totalCount = await query.CountAsync();

                var sortedQuery = queryDto.IsEmpty() ? query.OrderByDescending(c => c.CreatedAt) : _sorterService.SortData(query, queryDto);

                paginationDto.Validate();
                var paginatedQuery = sortedQuery.Skip(paginationDto.Skip).Take(paginationDto.Take);

                var quizzes = await paginatedQuery.ToListAsync(token);

                if(quizzes.Count == 0)
                {
                    return NotFound(new { Message = "No quizzes found!" });
                }
                var result = quizzes.Select(quiz =>
                {
                    var howMany = allProgressQuizzes.Where(c => c.QuizId == quiz.ID && c.Completed == true).Count();

                    return new
                    {
                        quiz.ID,
                        quiz.QuizName,
                        quiz.QuizDescription,
                        quiz.UserId,
                        quiz.QuizCategory,
                        quiz.ViewCount,
                        HowMany = howMany,
                        quiz.CreatedAt,
                        quiz.LastModified
                    };
                });
                bool hasMore = (paginationDto.Skip + quizzes.Count) < totalCount;


                return Ok(new {result, hasMore});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retrieving quizzes for user {userId}");
                return BadRequest(new { Message = "Erro in retriving quizzes" });
            }
        }

        [HttpGet("/api/Quizzes/GetAll")]
        public async Task<IActionResult> GetAllQuizzes([FromQuery] PaginationDto paginationDto, [FromQuery] int? categoryId, [FromQuery] SortQueryDto queryDto, [FromQuery] int? userId, CancellationToken token)
        {
            try
            {
                var query = _context.Quizzes.AsNoTracking();

                var allProgressQuizzesByUserId = 
                    userId.HasValue ? await _quizzesCompletedRepository
                        .GetAll()
                        .AsNoTracking()
                        .Where(c => c.UserId == userId)
                        .Select(c => new
                        {
                            c.QuizId,
                            c.Completed
                        })
                        .ToListAsync(token)
                    : null;

                var allProgressQuizzes = await _quizzesCompletedRepository.GetAll().AsNoTracking().ToListAsync(token);

                if (categoryId.HasValue)
                {
                    query = query.Where(c => c.QuizCategory == categoryId.Value);
                }
                var totalCount = await query.CountAsync();

                var sortedQuery = queryDto.IsEmpty() ? query.OrderByDescending(c => c.CreatedAt) : _sorterService.SortData(query, queryDto);

                paginationDto.Validate();
                var paginatedQuery = sortedQuery.Skip(paginationDto.Skip).Take(paginationDto.Take);

                var quizzes = await paginatedQuery
                    //.Include(c => c.Questions)
                    //.ThenInclude(c => c.Answers)
                    .ToListAsync(token);
                
                if(quizzes.Count == 0)
                {
                    return NotFound(new { Message = "No quizzes found" });
                }

                var result = quizzes.Select(quiz =>
                {
                    var status = allProgressQuizzesByUserId?.FirstOrDefault(c => c.QuizId == quiz.ID);
                    var howManyTimes = allProgressQuizzes?.Where(c => c.QuizId == quiz.ID && c.Completed == true).Count();

                    var isCompleted = status != null && status.Completed; 
                    return new
                    {
                        quiz.ID,
                        quiz.QuizName,
                        quiz.QuizDescription,
                        quiz.UserId,
                        quiz.QuizCategory,
                        Status = isCompleted,
                        HowMany = howManyTimes,
                        quiz.CreatedAt,
                        quiz.LastModified
                        //Question = quiz.Questions.Select(question => new
                        //{
                        //    question.ID,
                        //    question.QuestionText,
                        //    question.QuizId,
                        //    question.QuestionType,
                        //    Answer = question.Answers.Select(answer => new
                        //    {
                        //        answer.ID,
                        //        answer.AnswerText,
                        //        answer.IsCorrect,
                        //        answer.QuestionId,
                        //        answer.CreatedAt,
                        //        answer.LastModified
                        //    }).ToList()
                        //}).ToList()
                    };
                });
                bool hasMore = (paginationDto.Skip + quizzes.Count) < totalCount;

                return Ok(new {result, hasMore});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving quizzes");
                return BadRequest(new { Message = "Error in retrviign quizies" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSingleQuiz(int id, [FromQuery] int? userId, CancellationToken token)
        {
            try
            {
                var quiz = await _quizzesRepository.GetAll().AsNoTracking().Include(c => c.Questions).ThenInclude(c => c.Answers).FirstOrDefaultAsync(c => c.ID == id, token);
                var mistakes = userId.HasValue ? await _quizzesCompletedRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && c.QuizId == id) : null;
                if(quiz == null)
                {
                    return NotFound(new { Message = "Not found quiz" });
                }
                return Ok(new { Quiz = quiz, mistakes?.Mistakes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in getting single quiz with id {id}");
                return BadRequest(new { Message = "Error in getting single quiz" });
            }
        }

        [HttpGet("/api/Quizzes/UserQuizInfo/{userId}")]
        public async Task<IActionResult> GetUserQuizInfo(int userId, CancellationToken token)
        {
            try
            {
                var userInfo = await _userRepository.Get(userId, token);
                var userResponse = new
                {
                    Name = userInfo.Firstname + ' ' + userInfo.Lastname,
                    userInfo.Firstname,
                    userInfo.Lastname,
                    userInfo.ProfilePictureUrl,
                    userInfo.ID
                };
                var quizzesCount = await _quizzesRepository.CountAsync(c => c.UserId == userId, token);
                return Ok(new {Info = userResponse, Count = quizzesCount});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving quizzes");
                return BadRequest(new {Message="Error in retriving quizzes"});
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("AdminDelete/{id}")]
        public async Task<IActionResult> DeleteQuizAdmin(int id, CancellationToken token)
        {
            try
            {
                var quiz = await _quizzesRepository.Get(id, token);
                if (quiz == null)
                {
                    return NotFound(new { Message = "Quiz not found" });
                }
                await _quizzesRepository.Delete(quiz.ID, token);
                await _quizzesRepository.SaveAsync(token);
                return Ok(new { Message = "Quiz deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting quiz with id {id}");
                return BadRequest(new { Message = "Error in deleting quiz" });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuiz(int id, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var quiz = await _quizzesRepository.Get(id, token);
                if(quiz == null)
                {
                    return NotFound(new { Message = "Quiz not found" });
                }
                if(quiz.UserId != userId)
                {
                    return Forbid();
                }

                await _quizzesRepository.Delete(quiz.ID, token);
                await _quizzesRepository.SaveAsync(token);
                return Ok(new { Message = "Quiz deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting quiz with id {id}");
                return BadRequest(new { Message = "Error in deleting quiz" });
            }
        }
    }
}
