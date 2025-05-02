using Database.Context;
using Database.DTOs;
using Database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

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

        [HttpPost("BecomeInstructor")]
        public async Task<IActionResult> BecomeInstructor([FromBody] CreateInstructor instructorDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = await _context.Users.FindAsync(instructorDto.UserId, token);
                if(user == null)
                {
                    return NotFound();
                }
                foreach (var social in instructorDto.Socials)
                {
                    if(string.IsNullOrWhiteSpace(social.Label) || string.IsNullOrWhiteSpace(social.Link))
                    {
                        return BadRequest(new { Message = "no null data" });
                    }
                }
                var serializeSocials = JsonSerializer.Serialize(instructorDto.Socials);

                var newInstructor = new Instructors
                {
                    UserId = instructorDto.UserId,
                    Expertise = instructorDto.Expertise,
                    Bio = instructorDto.Bio,
                    Socials = serializeSocials,
                    LastModified = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Instructors.AddAsync(newInstructor, token);
                user.Role = "Instructor";
                user.LastModified = DateTime.UtcNow;
                _context.Users.Update(user);

                await _context.SaveChangesAsync(token);

                await transaction.CommitAsync(token);

                return Ok(new { Message = "User became instructor" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in becoming an instructor");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Instructor")]
        [HttpPost("CreateCourse")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto courseDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(userId == null)
                {
                    return Unauthorized();
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var user = await _context.Users
                    .Select(c => new
                    {
                        c.ID,
                        InstructorId = c.Instructor.ID
                    })
                    .FirstOrDefaultAsync(c => c.ID == Int32.Parse(userId));
                if(user == null)
                {
                    return NotFound(new {Message = "No user"});
                }

                if(courseDto.SectionTitles.Count != courseDto.SectionLessons.Count)
                {
                    return BadRequest(new { Message = "Not same lengths" });
                }

                string topics = JsonSerializer.Serialize(courseDto.TopicsCovered);

                var newCourse = new InstructorCourses
                {
                    InstructorId = user.InstructorId,
                    Name = courseDto.Name,
                    Description = courseDto.Description,
                    TopicsCovered = topics,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    InstructorCourseSections = new List<InstructorCourseSections>()
                };

                for (int i = 0; i < courseDto.SectionTitles.Count; i++)
                {
                    var newSection = new InstructorCourseSections
                    {
                        Course_Id = newCourse.ID,
                        Title = courseDto.SectionTitles[i],
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        InstructorLessons = new List<InstructorLessons>()
                    };

                    foreach (var lessonTitle in courseDto.SectionLessons[i])
                    {
                        var newLesson = new InstructorLessons
                        {
                            Title = lessonTitle,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        };
                        newSection.InstructorLessons.Add(newLesson);
                    }
                    newCourse.InstructorCourseSections.Add(newSection);
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

        //kjo osht kur te fillon ni kurs qe e ofron instruktori ose kur kyqet me ane te url
        [Authorize]
        [HttpPost("StartCourse")]
        public async Task<IActionResult> EnrollCourse([FromBody] EnrollCourseDto enrollCourse, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(user == null)
                {
                    return Unauthorized();
                }

                var userId = Int32.Parse(user);

                var getStudentAvailable = await _context.InstructorStudents.FirstOrDefaultAsync(c => c.UserId == userId && c.InstructorId == enrollCourse.InstructorId);
                var getLessonProgress = await _context.StudentCourseLessonProgress.FirstOrDefaultAsync(c => c.UserId == userId && c.OnlineMeetId == enrollCourse.OnlineMeetId);

                if (getStudentAvailable == null)
                {
                    var newEnrollment = new InstructorStudents
                    {
                        UserId = userId,
                        CourseId = enrollCourse.CourseId,
                        InstructorId = enrollCourse.InstructorId,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                await _context.InstructorStudents.AddAsync(newEnrollment, token);
                }

                if(getLessonProgress == null)
                {
                    var newLessonProgress = new StudentCourseLessonProgress
                    {
                        UserId = userId,
                        OnlineMeetId = enrollCourse.OnlineMeetId,
                        HasJoined = false,
                        IsCompleted = false,
                        JoinedTime = null,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                await _context.StudentCourseLessonProgress.AddAsync(newLessonProgress, token);
                }

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

        [Authorize(Roles = "Instructor")]
        [HttpGet("GetInstructorCoursesForMeetingAdd")]
        public async Task<IActionResult> GetAllCoursesForMeetingAdd()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(userId == null)
                {
                    return Unauthorized();
                }
                var user = await _context.Users
                    .Select(c => new
                    {
                        c.ID,
                        InstructorId = c.Instructor.ID
                    })
                    .FirstOrDefaultAsync(c => c.ID == Int32.Parse(userId));

                if(user == null)
                {
                    return NotFound(new {Message = "No user found"});
                }

                var courses = await _context.InstructorCourses.Where(c => c.InstructorId == user.InstructorId).ToListAsync();
                if(courses.Count == 0)
                {
                    return NotFound(new {Message = "No courses found"});
                }

                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor courses");
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
                    .FirstOrDefaultAsync();

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

        [HttpDelete("CourseDelete/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.InstructorCourses.FindAsync(id);
                if (course == null)
                {
                    return NotFound();
                }
                _context.Remove(course);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Course deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course");
                return BadRequest();
            }
        }

        
    }
}
