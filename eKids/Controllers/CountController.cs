using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountController : ControllerBase
    {
        private readonly IRepository<Courses> _courseRepository;
        private readonly IRepository<Lessons> _lessonRepository;
        private readonly ILogger<CountController> _logger;
        private readonly IViewCountService _viewCountService;
        private readonly IRepository<Comments> _commentsRepository;

        public CountController(IRepository<Courses> courseRepository, IRepository<Comments> commentsRepository, IRepository<Lessons> lessonRepository, ILogger<CountController> logger, IViewCountService viewCountService)
        {
            _courseRepository = courseRepository;
            _lessonRepository = lessonRepository;
            _commentsRepository = commentsRepository;
            _logger = logger;
            _viewCountService = viewCountService;
        }

        [HttpPost("increment/{id}")]
        public IActionResult IncrementViewCount(int id, string postType)
        {
            //post type can be course, lesson, category
            try
            {
                _viewCountService.IncrementViewCount(id, postType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating view count for :{postType} with Id: {id}");
                return BadRequest(new { message = "Not updated count!" });
            }
            return Ok(new { message = "View count incremented in Redis successfully." });
        }

    }
}
