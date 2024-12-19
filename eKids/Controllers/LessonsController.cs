using AutoMapper;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using eKids.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
        private readonly IMapper _mapper;
        private readonly ILessonLikesService _lessonLikeService;
        private readonly ICommentService _commentService;
        private readonly ILessonNavigationService _lessonNavigationService;
        private readonly IRepository<Bookmarks> _bookmarkRepository;

        public LessonsController(IRepository<Lessons> lessonRepository, IRepository<Bookmarks> bookmarkRepository, ILessonNavigationService lessonNavigationService, ICommentService commentService, ILogger<LessonsController> logger, IFileUploadService fileUploadService, IMapper mapper, ILessonLikesService lessonLikesService)
        {
            _lessonRepository = lessonRepository;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _mapper = mapper;
            _lessonLikeService = lessonLikesService;
            _commentService = commentService;
            _lessonNavigationService = lessonNavigationService;
            _bookmarkRepository = bookmarkRepository;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLessons(int id, [FromQuery] int userId, CancellationToken token)
        {
            try
            {
                var lesson = await _lessonRepository.Get(id, token, u => u.Course);
                var isLikes = await _lessonLikeService.GetLessonLikeByUser(id, userId, token);
                var countLessonComments = await _commentService.GetAllLessonCommentsCount(id, token);
                var getLessonNavigations = await _lessonNavigationService.GetLessonNavigationAsync(id, token);
                var getCurrentLessonStatus = await _lessonNavigationService.GetLessonCompletation(id, userId, token);
                var bookmarks = await _bookmarkRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && c.LessonId == id, token);
                var isBookmarked = bookmarks != null;

                bool isLiked = isLikes != null;
                if(lesson == null)
                {
                    return NotFound(new { Message = "No lesson found!" });
                }
                return Ok(new { lesson, isLiked, countLessonComments, navigation = getLessonNavigations, currentProgress = getCurrentLessonStatus, isBookmarked });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lesson!");
                var errorMessage = new { Message = "Error in retrieving lesson!" };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
        }

        [HttpPatch("/api/Lessons/UpdateLike/{id}")]
        public async Task<IActionResult> UpdateLessonLike(int id, [FromQuery] int userId, CancellationToken token)
        {
            try
            {
                var lesson = await _lessonRepository.Get(id, token);
                if(lesson == null)
                {
                    return BadRequest(new { Message = "No lesson found!" });
                }

                var getIsLiked = await _lessonLikeService.GetLessonLikeByUser(id, userId, token);
                if(getIsLiked != null)
                {
                    await _lessonLikeService.RemoveUserLessonLikeAsync(getIsLiked, token);
                    lesson.Likes = Math.Max(0, lesson.Likes - 1);
                    await _lessonRepository.SaveAsync(token);
                    return Ok(new { Message = "Lesson Like removed", lesson.Likes });
                }
                else
                {
                    await _lessonLikeService.AddUserLessonLikeAsync(id, userId, token);
                    lesson.Likes += 1;
                    await _lessonRepository.SaveAsync(token);
                    return Ok(new { Message = "Lesson Like added", lesson.Likes });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating lesson like with id: {id}");
                return BadRequest(new { Message = "Error updating lessonLike" });
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
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLessons(int id, [FromBody] UpdateLessons lessonDto)
        {

            try
            {
                var lesson = await _lessonRepository.Get(id, default);

                if(lesson == null)
                {
                    return NotFound(new { Message = "No Lesson found!" });
                }

                _mapper.Map(lessonDto, lesson);

                if (!string.IsNullOrEmpty(lessonDto.LessonFeaturedImage))
                {
                    try
                    {
                        var imageUrl = await _fileUploadService.UploadFile(lessonDto.LessonFeaturedImage, FileCategory.Other);
                        var url = $"{Request.Scheme}://{Request.Host}{imageUrl}";
                        lessonDto.LessonFeaturedImage = url;
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex);
                    }
                }

                if (!string.IsNullOrEmpty(lessonDto.LessonVideo))
                {
                    try
                    {
                        var videoUrl = await _fileUploadService.UploadFile(lessonDto.LessonVideo, FileCategory.Videos);
                        var vUrl = $"{Request.Scheme}://{Request.Host}{videoUrl}";
                        lessonDto.LessonVideo = vUrl;
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex);
                    }
                }

                _lessonRepository.Update(lesson);
                await _lessonRepository.SaveAsync(default);

                return Ok(lesson);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating lesson with ID: {id}");
                var errorMessage = new
                {
                    Message = "Error updating course!"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }



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
