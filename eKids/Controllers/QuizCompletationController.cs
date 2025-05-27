using Database.DTOs;
using Database.Models;
using Database.Repository;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizCompletationController : ControllerBase
    {
        private readonly IRepository<QuizzesCompleted> _quizCompletationRep;
        private readonly ILogger<QuizCompletationController> _logger;

        public QuizCompletationController(IRepository<QuizzesCompleted> quizCompletationRep, ILogger<QuizCompletationController> logger)
        {
            _quizCompletationRep = quizCompletationRep;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuizCompleteStarted(QuizCompStartDto quizCompDto, CancellationToken token)
        {
            try
            {
                if(quizCompDto == null)
                {
                    return BadRequest("Data missing");
                }

                var exists = await _quizCompletationRep.IsExist(c => c.QuizId == quizCompDto.QuizId && c.UserId == quizCompDto.UserId, token);

                if (exists)
                {
                    return Conflict(new { Message = "Quiz already started" });
                }

                var quizCompleted = new QuizzesCompleted
                {
                    UserId = quizCompDto.UserId,
                    QuizId = quizCompDto.QuizId,
                    Completed = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };

                _quizCompletationRep.Add(quizCompleted);
                await _quizCompletationRep.SaveAsync(token);

                return Ok(new { Message = "Quiz started" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in starting Quiz");
                return BadRequest(new { Message = "Error in starting quiz" });
            }
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateQuizCompletationStatus(QuizCompletationDto quizComp, CancellationToken token)
        {
            try
            {
                var quiz = await _quizCompletationRep.GetAll().FirstOrDefaultAsync(c => c.QuizId == quizComp.QuizId && c.UserId == quizComp.UserId, token);
                if(quiz == null)
                {
                    return NotFound(new { Message = "Quiz not found" });
                }
                quiz.Completed = quizComp.Completed;
                //quiz.Mistakes = quizComp.Mistakes;
                //quiz.Duration = quizComp.Duration;
                quiz.LastModified = DateTime.UtcNow;

                _quizCompletationRep.Update(quiz);
                await _quizCompletationRep.SaveAsync(token);

                return Ok(new { Message = "Successfully status updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updaing quiz completation status with id {quizComp.QuizId}");
                return BadRequest(new {Message="Error updating quiz completation status"});
            }
        }

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

        [HttpDelete]
        public async Task<IActionResult> DeleteQuizCompletation(int id, CancellationToken token)
        {
            try
            {
                var quiz = await _quizCompletationRep.Get(id, token);
                if(quiz == null)
                {
                    return NotFound(new { Message = "Not found" });
                }

                await _quizCompletationRep.Delete(quiz.ID, token);
                await _quizCompletationRep.SaveAsync(token);
                return Ok(new { Message = "Quiz deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in deleting quiz compeltation status");
                return BadRequest(new { Message = "Error deleting quiz completation status" });
            }
        }

        [HttpPatch("/api/QuizzesCompleted/UpdateQuizMistakes/")]
        public async Task<IActionResult> UpdateMistakes(QuizCompStartDto updateMistakesDto, CancellationToken token)
        {
            try
            {
                var quiz = await _quizCompletationRep.GetAll().AsNoTracking().FirstOrDefaultAsync(c => c.QuizId == updateMistakesDto.QuizId && c.UserId == updateMistakesDto.UserId, token);
                if(quiz == null)
                {
                    return NotFound(new { Message = "Not found quiz" });
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

        [HttpGet("/api/QuizzesCompletation/GetStatusQuizz/{userId}/{quizId}")]
        public async Task<IActionResult> GetStatusOfQuiz(int userId, int quizId, CancellationToken token)
        {
            try
            {
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
