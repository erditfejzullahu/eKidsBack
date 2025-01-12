using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProgressController : ControllerBase
    {
        private readonly IRepository<UserProgress> _userProgressRepository;
        private readonly ILogger<UserProgressController> _logger;
        private readonly IRepository<Courses> _coursesRepository;
        private readonly IRepository<Lessons> _lessonRepository;
        private readonly ICourseCompletationService _courseCompletationService;

        public UserProgressController(
            IRepository<UserProgress> userProgressRepository,
            ILogger<UserProgressController> logger,
            IRepository<Courses> coursesRepository,
            IRepository<Lessons> lessonsRepository,
            ICourseCompletationService courseCompletationService
            )
        {
            _userProgressRepository = userProgressRepository;
            _logger = logger;
            _coursesRepository = coursesRepository;
            _lessonRepository = lessonsRepository;
            _courseCompletationService = courseCompletationService;
        }

        [HttpGet("/api/UserProgresses/{userId}")]
        public async Task<IActionResult> GetProgresses(int userId, CancellationToken token)
        {
            try
            {
                var userProgress = await _userProgressRepository
                    .GetAll()
                    .Where(c => c.UserId == userId)
                    .ToListAsync(token);

                var courseIds = userProgress.Select(progress => progress.CourseId).Distinct().ToList();

                var courseDetailsList = await _coursesRepository
                    .GetAll()
                    .Where(c => courseIds.Contains(c.ID))
                    .Include(c => c.Lessons)
                    .ToListAsync(token);

                var response = courseDetailsList.Select(course => new
                {
                    courseId = course.ID,
                    courseName = course.CourseName,
                    courseCategory = course.CourseCategory,
                    courseImage = course.CourseFeaturedImage,
                    lessonDetails = course.Lessons.Select(lesson => new
                    {
                        lessonId = lesson.ID,
                        lessonName = lesson.LessonName,
                        progress = userProgress
                            .Where(progress => progress.LessonId == lesson.ID)
                            .Select(progress => new
                            {
                                progressId = progress.ID,
                                lessonProgressStarted = progress.HasStarted,
                                lessonProgressCompleted = progress.IsCompleted
                            }).ToList()
                    }).ToList(),
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"No Progress found with ID: {userId}");
                var errorMessage = new { Message = "Error retrieving progress" };
                return BadRequest(errorMessage);
            }
        }

        [HttpGet("{courseId}/{userId}")]
        public async Task<IActionResult> GetStatus(int courseId, int userId, CancellationToken token)
        {
            try
            {
                var userProgress = await _userProgressRepository
                    .GetAll()
                    .Where(c => c.UserId == userId && c.CourseId == courseId)
                    .ToListAsync(token);
                if (userProgress == null || !userProgress.Any())
                {
                    return BadRequest(new { Message = "No progress found for the specified user and course." });
                }
                var courseDetails = await _coursesRepository.Get(courseId, token, c => c.Lessons);

                var response = new
                {
                    cId = courseDetails.ID,
                    cName = courseDetails.CourseName,
                    UserProgress = userProgress.Select(progress => new
                    {
                        progressId = progress.ID,
                        progressLessonId = progress.LessonId,
                        progressLessonCompleted = progress.IsCompleted,
                        progressLessonStarted = progress.HasStarted,
                        progressLessonName = courseDetails.Lessons?
                            .FirstOrDefault(lesson => lesson.ID == progress.LessonId)?.LessonName
                    })

                };
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"No Progress found with ID: {courseId}");
                var errorMessage = new { Message = "Error retrieving progress" };
                return BadRequest(errorMessage);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProgress(CreateUserProgress progressDto, CancellationToken token)
        {
            if(progressDto == null)
            {
                return BadRequest();
            }

            try
            {
                var allLessonsByCourseId = await _userProgressRepository.GetLessonsByCourseId(progressDto.CourseId).ToListAsync(token);

                if (allLessonsByCourseId.Count == 0)
                {
                    return BadRequest();
                }
                var lesson = await _lessonRepository.Get(progressDto.LessonId, token);
                var course = await _coursesRepository.Get(progressDto.CourseId, token);
                if (lesson == null || course == null)
                {
                    return BadRequest(new { Message = "Course not enrolled bc lesson id missing!" });
                }
                
                var userProgressList = allLessonsByCourseId.Select((lesson, index ) => new UserProgress
                {
                    UserId = progressDto.UserId,
                    LessonId = lesson.ID,
                    CourseId = progressDto.CourseId,
                    IsCompleted = false,
                    HasStarted = index == 0,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                }).ToList();
                _userProgressRepository.AddRange(userProgressList);

                course.CourseEnrolled += 1;
                _coursesRepository.Update(course);
                await _coursesRepository.SaveAsync(token);

                lesson.LessonStarted += 1;
                _lessonRepository.Update(lesson);
                await _lessonRepository.SaveAsync(token);
                
                await _userProgressRepository.SaveAsync(token);
                return Ok(userProgressList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating user progress");
                return BadRequest(new { Message=$"Error creating user progress"});
            }
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateUserProgress(UpdateUserProgress updateUserProgressDto, CancellationToken token)
        {
            try
            {
                var lesson = await _userProgressRepository
                    .GetAll()
                    .Where(c => c.CourseId == updateUserProgressDto.CourseId && c.LessonId == updateUserProgressDto.LessonId && c.UserId == updateUserProgressDto.UserId)
                    .FirstOrDefaultAsync(token);
                if(lesson == null)
                {
                    return BadRequest(new { Message = "No lesson found!" });
                }

                if (updateUserProgressDto.IsCompleted.HasValue)
                {
                    lesson.IsCompleted = updateUserProgressDto.IsCompleted ?? lesson.IsCompleted;
                }

                if (updateUserProgressDto.HasStarted.HasValue)
                {
                    lesson.HasStarted = updateUserProgressDto.HasStarted ?? lesson.HasStarted;
                }

                lesson.LastModified = DateTime.UtcNow;
                _userProgressRepository.Update(lesson);
                await _userProgressRepository.SaveAsync(token);

                var allLessonsCompelted = await _userProgressRepository
                    .GetAll()
                    .Where(c => c.UserId == updateUserProgressDto.UserId && c.CourseId == updateUserProgressDto.CourseId)
                    .AllAsync(c => c.IsCompleted, token);

                if (allLessonsCompelted)
                {
                    var completionResponse = await _courseCompletationService.CompleteCourse(updateUserProgressDto.CourseId, updateUserProgressDto.UserId, token);
                    return Ok(completionResponse);
                }

                return Ok(lesson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating lesson ptogress");
                return BadRequest(new { Message = "Error updating lesson!" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgress(int id, CancellationToken token)
        {
            try
            {
                var progress = await _userProgressRepository.Get(id, token);
                if(progress == null)
                {
                    return BadRequest(new { Message = "No data found" });
                }
                await _userProgressRepository.Delete(progress.ID, token);
                await _userProgressRepository.SaveAsync(token);
                return Ok(new { Message = "progress deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user course progress with ID: {id}");
                return BadRequest(new { Message = "Error deleting progress" });
            }
        }
    }
}
