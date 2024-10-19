using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly IRepository<Lessons> _lessonRepository;
        private readonly ILogger<LessonsController> _logger;
        private readonly IFileUploadService _fileUploadService;

        public LessonsController(IRepository<Lessons> lessonRepository, ILogger<LessonsController> logger, IFileUploadService fileUploadService)
        {
            _lessonRepository = lessonRepository;
            _logger = logger;
            _fileUploadService = fileUploadService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLessons(int id)
        {
            try
            {
                var lesson = await _lessonRepository.Get(id, default);
                if(lesson == null)
                {
                    return NotFound(new { Message = "No lesson found!" });
                }
                return Ok(lesson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lesson!");
                var errorMessage = new { Message = "Error in retrieving lesson!" };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateLessons([FromBody] CreateLessons lessonDto)
        {
            if (lessonDto == null)
            {
                return BadRequest("Lesson data is null");
            }

            try
            {
                string relativeUrl = await _fileUploadService.UploadFile(lessonDto.LessonFeaturedImage, FileCategory.Other);
                var imageUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
                var lesson = new Lessons
                {
                    LessonName = lessonDto.LessonName,
                    LessonContent = lessonDto.LessonContent,
                    LessonExcerpt = lessonDto.LessonExcerpt,
                    LessonFeaturedImage = imageUrl,
                    CourseID = lessonDto.CourseID,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };


                //check for quiz 
                if (lessonDto.HasQuiz)
                {
                    lesson.LessonType = lessonDto.LessonType;
                    lesson.LessonQuestions = lessonDto.LessonQuestions;
                    lesson.LessonAnswers = lessonDto.LessonAnswers;
                    lesson.CorrectAnswers = lessonDto.CorrectAnswers;
                }
                else
                {
                    lesson.LessonType = string.Empty;
                    lesson.LessonQuestions = string.Empty;
                    lesson.LessonAnswers = string.Empty;
                    lesson.CorrectAnswers = string.Empty;
                }
                //check for quiz 

                //check for video
                if (!string.IsNullOrEmpty(lessonDto.LessonVideo))
                {
                    try
                    {
                        string videoUpload = await _fileUploadService.UploadFile(lessonDto.LessonVideo, FileCategory.Videos);
                        var videoUrl = $"{Request.Scheme}://{Request.Host}{videoUpload}";
                        lesson.LessonVideo = videoUrl;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in file");
                        var errorMessage = new
                        {
                            Message = "Error in file"
                        };
                        return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                    }
                }
                else
                {
                    lesson.LessonVideo = string.Empty;   
                }
                //check for video

                _lessonRepository.Add(lesson);
                await _lessonRepository.SaveAsync(default);
                return Ok(lesson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating lesson");
                var errorMessage = new
                {
                    Message = "Error in creating lesson"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
            
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLessons(int id, [FromBody] CreateLessons lessonDto)
        {
            var lesson = await _lessonRepository.Get(id, default);

            if(lesson == null)
            {
                return NotFound("No Lesson found");
            }

            lesson.LessonName = lessonDto.LessonName;
            lesson.LessonContent = lessonDto.LessonContent;
            lesson.LessonExcerpt = lessonDto.LessonExcerpt;
            lesson.LessonQuestions = lessonDto.LessonQuestions;
            lesson.LessonAnswers = lessonDto.LessonAnswers;
            lesson.LessonType = lessonDto.LessonType;
            lesson.LessonAnswers = lessonDto.LessonAnswers;
            lesson.CorrectAnswers = lessonDto.CorrectAnswers;

            if(!string.IsNullOrEmpty(lessonDto.LessonFeaturedImage))
            {

                try
                {
                    string relativeUrl = await _fileUploadService.UploadFile(lessonDto.LessonFeaturedImage, FileCategory.Other);
                    var url = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
                    lesson.LessonFeaturedImage = url;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in uploading lesson featured image with ID: {id}");
                    var errorMessage = new
                    {
                        Message = "Error in updating category image",
                    };
                    return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                }
            }

            _lessonRepository.Update(lesson);
            await _lessonRepository.SaveAsync(default);

            return Ok(lesson);

        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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

        [HttpGet("allLessons")]
        public async Task<IActionResult> GetAllLessons(CancellationToken token)
        {
            try
            {
                var lessons = await _lessonRepository
                    .GetAll()
                    .ToListAsync(token);
                if(lessons == null)
                {
                    return BadRequest(new { Message = "No lessons Found!" });
                }
                return Ok(lessons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all lessons");
                var errorMessage = new
                {
                    Message = "Error in retrieving lessons!"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
            
        }
    }
}
