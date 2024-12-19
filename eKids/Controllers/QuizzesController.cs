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

        public QuizzesController(IRepository<Quizzes> quizzesRepository,
            ILogger<QuizzesController> logger,
            IRepository<QuizQuestions> quizQuestionsRep,
            IRepository<QuizAnswers> quizAnswersRep)
        {
            _logger = logger;
            _quizzesRepository = quizzesRepository;
            _quizQuestionsRep = quizQuestionsRep;
            _quizAnswersRep = quizAnswersRep;   
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
        public async Task<IActionResult> GetAllQuizzesByUser(int userId, CancellationToken token)
        {
            try
            {
                var quizzes = await _quizzesRepository.GetAll().AsNoTracking().Where(c => c.UserId == userId).Include(c => c.Questions).ThenInclude(c => c.Answers).ToListAsync(token);
            
                if(quizzes.Count == 0)
                {
                    return NotFound(new { Message = "No quizzes found!" });
                }
                return Ok(quizzes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retrieving quizzes for user {userId}");
                return BadRequest(new { Message = "Erro in retriving quizzes" });
            }
        }

        [HttpGet("/api/Quizzes/GetAll")]
        public async Task<IActionResult> GetAllQuizzes([FromQuery] string? orderBy,[FromQuery] int? categoryId, CancellationToken token)
        {
            try
            {
                var query = _quizzesRepository.GetAll().AsNoTracking();

                if (!string.IsNullOrEmpty(orderBy))
                {
                    if(orderBy.Equals("desc", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.OrderByDescending(c => c.QuizName);
                    }else if (orderBy.Equals("asc", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.OrderBy(c => c.QuizName);
                    }
                    else
                    {
                        query = query.OrderBy(c => Guid.NewGuid());
                    }
                }


                if (categoryId.HasValue)
                {
                    query = query.Where(c => c.QuizCategory == categoryId.Value);
                }

                var quizzes = await query
                    .Include(c => c.Questions)
                    .ThenInclude(c => c.Answers)
                    .ToListAsync(token);

                if(quizzes.Count == 0)
                {
                    return NotFound(new { Message = "No quizzes found" });
                }
                return Ok(quizzes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving quizzes");
                return BadRequest(new { Message = "Error in retrviign quizies" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSingleQuiz(int id, CancellationToken token)
        {
            try
            {
                var quiz = await _quizzesRepository.GetAll().AsNoTracking().Include(c => c.Questions).ThenInclude(c => c.Answers).FirstOrDefaultAsync(c => c.ID == id, token);
                if(quiz == null)
                {
                    return NotFound(new { Message = "Not found quiz" });
                }
                return Ok(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in getting single quiz with id {id}");
                return BadRequest(new { Message = "Error in getting single quiz" });
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
