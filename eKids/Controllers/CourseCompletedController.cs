using Database.Models;
using Database.Repository;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eKids.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class CourseCompletedController : ControllerBase
    {
        private readonly IRepository<CourseCompleted> _courseCompletedRepository;
        private readonly ILogger<CourseCompletedController> _logger;

        public CourseCompletedController(IRepository<CourseCompleted> courseCompletedRepository, ILogger<CourseCompletedController> logger)
        {
            _courseCompletedRepository = courseCompletedRepository;
            _logger = logger;
        }

        //courses completed based of userid
        [HttpGet("/api/CourseCompleted")]
        public async Task<IActionResult> GetAllCoursesCompleted([FromQuery] int userId, CancellationToken token)
        {
            try
            {

                var completations = await _courseCompletedRepository.GetAll().Include(c => c.Course).AsNoTracking().Where(c => c.UserId == userId).ToListAsync(token);
                if(completations.Count == 0)
                {
                    return NotFound(new {Message="No completation found!"});
                }
                return Ok(completations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retriving courses");
                return BadRequest(new { Message = "Error in retriving courses!" });
            }
        }

        //courses completation certificate based of course
        [HttpGet("{courseId}/{userId}")]
        public async Task<IActionResult> GetCompletedCourse(int courseId, int userId, CancellationToken token)
        {
            try
            {
                var courseCompleted = await _courseCompletedRepository.GetAll().Include(c => c.Course).FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId, token);
                if(courseCompleted == null)
                {
                    return NotFound(new { Message = "No course compelted found" });
                }
                return Ok(courseCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retriving course with id:{courseId} and user:{userId}");
                return BadRequest(new { Message = "Error retriving completed course" });
            }
        }

        [Authorize]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateTestimonial(int id, [FromBody] string testimonialData, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var testimonial = await _courseCompletedRepository.GetAll().FirstOrDefaultAsync(c => c.ID == id && c.UserId == userId, token);
                if(testimonial == null)
                {
                    return NotFound(new { Message = "Testimonial not found!" });
                }
                if(testimonial.UserId != userId)
                {
                    return Forbid();
                }
                if (string.IsNullOrEmpty(testimonialData))
                {
                    return BadRequest(new { Message = "Testimonial data is required" });
                }
                var sanitizer = new HtmlSanitizer();
                testimonial.Testimonial = sanitizer.Sanitize(testimonialData.Trim());
                _courseCompletedRepository.Update(testimonial);
                await _courseCompletedRepository.SaveAsync(token);
                return Ok(new { Message = "Testimonial updated successfully", Testimonial = testimonial });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating testimonial with id{id}");
                return BadRequest(new { Message = "Error updating testimonial" });
            }
        }
    }
}
