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
    public class QuizzesController : ControllerBase
    {
        private readonly IRepository<Quizzes> _quizzesRepository;
        private readonly IRepository<QuizQuestions> _quizQuestionsRep;
        private readonly IRepository<QuizAnswers> _quizAnswersRep;
        private readonly ILogger<QuizzesController> _logger;
        private readonly IRepository<Users> _userRepository;
        private readonly IRepository<QuizzesCompleted> _quizzesCompletedRepository;
        private readonly ISorterService<Quizzes> _sorterService;

        public QuizzesController(IRepository<Quizzes> quizzesRepository,
            ILogger<QuizzesController> logger,
            IRepository<QuizQuestions> quizQuestionsRep,
            IRepository<QuizAnswers> quizAnswersRep,
            IRepository<Users> userRepository,
            IRepository<QuizzesCompleted> quizzesCompletedRepository,
            ISorterService<Quizzes> sorterService)
        {
            _logger = logger;
            _quizzesRepository = quizzesRepository;
            _quizQuestionsRep = quizQuestionsRep;
            _quizAnswersRep = quizAnswersRep;
            _userRepository = userRepository;
            _quizzesCompletedRepository = quizzesCompletedRepository;
            _sorterService = sorterService;
        }

        [HttpPost()]
        public async Task<IActionResult> CreateQuiz([FromBody] ProcessQuizDto quizDto, CancellationToken token)
        {
            try
            {
                var quiz = new Quizzes
                {
                    QuizName = quizDto.QuizTitle,
                    QuizDescription = quizDto.QuizDescription,
                    UserId = quizDto.UserId,
                    QuizCategory = quizDto.QuizCategory,
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now
                };
                _quizzesRepository.Add(quiz);
                await _quizzesRepository.SaveAsync(token);



                var questions = new List<QuizQuestions>();
                var answers = new List<QuizAnswers>();

                var questionMap = new Dictionary<int, QuizQuestions>();
                var answerMap = new Dictionary<int, QuizAnswers>();
                var answerIndexMap = new Dictionary<int, int>();

                foreach (var entry in quizDto.QuizData)
                {
                    var parts = entry.Key.Split("_");
                    int parentId = int.Parse(parts[2]);
                    

                    if (parts[3] == "question")
                    {

                        var question = new QuizQuestions
                        {
                            QuizId = quiz.ID,
                            QuestionText = entry.Value.ToString(),
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        };
                        questions.Add(question);
                        questionMap[parentId] = question;
                    }
                }

                _quizQuestionsRep.AddRange(questions);
                await _quizQuestionsRep.SaveAsync(token);

                foreach (var entry in quizDto.QuizData)
                {
                    var parts = entry.Key.Split("_");
                    int parentId = int.Parse(parts[2]);

                    if (parts[3] == "answers" && questionMap.ContainsKey(parentId))
                    {
                        var answer = new QuizAnswers
                        {
                            AnswerText = entry.Value.ToString(),
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
                        bool isCorrect = entry.Value.ToString().ToLower() == "true";
                        int answerPosition = int.Parse(parts[4]); // The position/index of the correct answer (0-based)

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
                        questionMap[parentId].QuestionType = entry.Value.ToString();
                        //}
                    }

                }
                _quizAnswersRep.AddRange(answers);

                await _quizAnswersRep.SaveAsync(token);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating quiz");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while creating the quiz." });
            }
        }

        [HttpGet("/api/Quizzes/GetByUser/{userId}")]
        public async Task<IActionResult> GetAllQuizzesByUser([FromQuery] PaginationDto paginationDto, int userId, [FromQuery] SortQueryDto queryDto, [FromQuery] int? categoryId, CancellationToken token)
        {
            try
            {
                var query = _quizzesRepository.GetAll().AsNoTracking();
                var allProgressQuizzes = await _quizzesCompletedRepository.GetAll().AsNoTracking().ToListAsync(token);

                if (categoryId.HasValue)
                {
                    query = query.Where(c => c.QuizCategory == categoryId.Value);
                }

                var sortedQuery = _sorterService.SortData(query, queryDto);

                paginationDto.Validate();
                var paginatedQuery = sortedQuery.Take(paginationDto.Take).Skip(paginationDto.Skip);

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

                return Ok(result);
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
                var query = _quizzesRepository.GetAll().AsNoTracking();

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

                var sortedQuery = _sorterService.SortData(query, queryDto);

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

                return Ok(result);
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

        [HttpDelete]
        public async Task<IActionResult> DeleteQuiz(int id, CancellationToken token)
        {
            try
            {
                var quiz = await _quizzesRepository.Get(id, token);
                if(quiz == null)
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
    }
}
