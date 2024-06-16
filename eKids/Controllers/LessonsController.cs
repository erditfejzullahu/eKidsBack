using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly IRepository<Lessons> _lessonRepository;

        public LessonsController(IRepository<Lessons> lessonRepository)
        {
            _lessonRepository = lessonRepository;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLessons(int id)
        {
            var lesson = await _lessonRepository.Get(id, default);
            if(lesson == null)
            {
                return NotFound();
            }
            return Ok(lesson);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLessons([FromBody] CreateLessons lessonDto)
        {
            if (lessonDto == null)
            {
                return BadRequest("Lesson data is null");
            }

            var lesson = new Lessons
            {
                LessonName = lessonDto.LessonName,
                CategoryID = lessonDto.CategoryID,
                LessonType = lessonDto.LessonType,
                LessonContent = lessonDto.LessonContent,
                LessonQuestions = lessonDto.LessonQuestions,
                LessonAnswers = lessonDto.LessonAnswers,
                CorrectAnswers = lessonDto.CorrectAnswers,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };

            _lessonRepository.Add(lesson);
            await _lessonRepository.SaveAsync(default);
            return Ok(lesson);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLessons([FromForm] CreateLessons lessonDto)
        {
            if(lessonDto == null)
            {
                return BadRequest("Lesson data is null");
            }

            var lesson = new Lessons
            {
                LessonName = lessonDto.LessonName,
                CategoryID = lessonDto.CategoryID,
                LessonType = lessonDto.LessonType,
                LessonContent = lessonDto.LessonContent,
                LessonQuestions = lessonDto.LessonQuestions,
                LessonAnswers = lessonDto.LessonAnswers,
                CorrectAnswers = lessonDto.CorrectAnswers,
                LastModified = DateTime.UtcNow,
            };
            _lessonRepository.Update(lesson);
            await _lessonRepository.SaveAsync(default);
            return Ok(lesson);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLessons(int id, CancellationToken token)
        {
            var lesson = await _lessonRepository.Get(id, default);
            if(lesson == null)
            {
                return NotFound();
            }

            await _lessonRepository.Delete(lesson.ID, token);
            await _lessonRepository.SaveAsync(default);
            return Ok(lesson);
        }
    }
}
