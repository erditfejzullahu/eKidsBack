using Database.Context;
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

        [HttpGet("Instructor/{id}")]
        public async Task<IActionResult> GetInstructorWithDetails(int id)
        {
            try
            {
                var instructor = await _context.Users
                    .Where(c => c.ID == id)
                    .AsSplitQuery()
                    .Include(c => c.Instructor)
                    .Select(c => new
                    {
                        c.ID,
                        Name = c.Firstname + " " + c.Lastname,
                        c.Email,
                        c.Instructor.Bio,
                        ProfilePicture = c.ProfilePictureUrl,
                        c.Instructor.Socials,
                        Joined = c.Instructor.CreatedAt,
                        TotalCourses = c.Instructor.InstructorCourses.ToList(),
                        TotalStudents = c.InstructorStudents.Where(s => s.UserId == id).Count(),
                    })
                    .ToListAsync();
                if(instructor == null)
                {
                    return NotFound(new {Message = "No instructor found"});
                }
                return Ok(instructor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in getting instructor with details");
                return BadRequest();
            }
        }
    }
}
