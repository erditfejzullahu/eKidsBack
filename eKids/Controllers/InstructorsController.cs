using Database.Context;
using Database.DTOs;
using Database.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstructorsController : ControllerBase
    {
        private readonly ILogger<InstructorsController> _logger;
        private readonly ApplicationDbContext _context;
        public InstructorsController(ILogger<InstructorsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpPost("CreateCourse")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto courseDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var newCourse = new InstructorCourses
                {
                    InstructorId = courseDto.InstructorId,
                    Name = courseDto.Name,
                    Description = courseDto.Description,
                    TopicsCovered = courseDto.TopicsCovered,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    InstructorCourseSections = new List<InstructorCourseSections>()
                };

                if(courseDto.sectionDtos.Count != 0)
                {
                    foreach (var section in courseDto.sectionDtos)
                    {
                        var newSection = new InstructorCourseSections
                        {
                            Course_Id = newCourse.ID,
                            Title = section.Title,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow,
                            InstructorLessons = new List<InstructorLessons>()
                        };
                        foreach(var lesson in section.lessonDtos)
                        {
                            var newLesson = new InstructorLessons
                            {
                                Section_Id = newSection.ID,
                                Title = lesson.Title,
                                Content = lesson.Content,
                                Video_Url = lesson.Video_Url,
                                CreatedAt = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow
                            };
                            newSection.InstructorLessons.Add(newLesson);
                        }
                        newCourse.InstructorCourseSections.Add(newSection);
                    }
                }
                await _context.InstructorCourses.AddAsync(newCourse, token);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Course added successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error in creating course");
                return BadRequest();
            }
        }

        [HttpPost("StartCourse")]
        public async Task<IActionResult> EnrollCourse([FromBody] EnrollCourseDto enrollCourse, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var newEnrollment = new InstructorStudents
                {
                    UserId = enrollCourse.UserId,
                    CourseId = enrollCourse.CourseId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                var newLessonProgress = new StudentCourseLessonProgress
                {
                    UserId = enrollCourse.UserId,
                    LessonId = enrollCourse.LessonId,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.InstructorStudents.AddAsync(newEnrollment, token);
                await _context.StudentCourseLessonProgress.AddAsync(newLessonProgress, token);

                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Enrolled successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error in enrollin course");
                return BadRequest();
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInstructorById(int id)
        {
            try
            {
                var instructor = await _context.Instructors
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(c => c.UserId == id)
                    .Select(c => new
                    {
                        c.UserId,
                        c.Expertise,
                        c.Bio,
                        c.Socials,
                        Name = c.User.Firstname + " " + c.User.Lastname,
                        c.User.Username,
                        c.User.Email,
                        c.User.Age,
                        c.User.ProfilePictureUrl,
                        Students = c.User.InstructorStudents
                        .Select(s => new
                        {
                            Name = s.User.Firstname + " " + s.User.Lastname,
                            s.User.ProfilePictureUrl,
                        })
                        .ToList(),
                        Courses = c.InstructorCourses.ToList(),
                        Friends = c.User.Friends
                        .Select(f => new
                        {
                            f.UserId,
                            Name = f.User.Firstname + " " + f.User.Lastname,
                            f.User.ProfilePictureUrl,
                        })
                        .ToList(),
                    })
                    .ToListAsync();

                if (instructor == null)
                {
                    return NotFound();
                }

                return Ok(instructor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor by id");
                return BadRequest();
            }
        }

        [HttpGet("Course/{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            try
            {
                var course = await _context.InstructorCourses
                    .Where(c => c.ID == id)
                    .Select(c => new
                    {
                        CourseId = c.ID,
                        c.InstructorId,
                        CourseName = c.Name,
                        CourseDescription = c.Description,
                        c.TopicsCovered,
                        IntructorName = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                        InstructorProfilePicture = c.Instructor.User.ProfilePictureUrl,
                        Sections = c.InstructorCourseSections.Select(ic => new
                        {
                            ic.ID,
                            Lessons = ic.InstructorLessons.Select(il => new
                            {
                                il.ID,
                                il.Title,
                                il.Content,
                                il.Video_Url,
                                il.CreatedAt,
                                il.LastModified,
                            }).ToList()
                        }).ToList(),
                    })
                    .ToListAsync();
                if(course == null)
                {
                    return NotFound(new {Message = "No course found"});
                }
                return Ok(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving course");
                return BadRequest();
            }
        }

        
    }
}
