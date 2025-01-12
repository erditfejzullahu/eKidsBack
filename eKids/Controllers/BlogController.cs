using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly ILogger<BlogController> _logger;
        private readonly ICreateBlogService _createBlogService;
        private readonly IRepository<Blogs> _blogRepository;
        private readonly IRepository<Tags> _tagsRepository;

        public BlogController(ILogger<BlogController> logger, IRepository<Tags> tagsRepository, ICreateBlogService createBlogService)
        {
            _logger = logger;
            _createBlogService = createBlogService;
            _tagsRepository = tagsRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromBody] CreateBlogDto request, CancellationToken token)
        {
            try
            {
                var blog = await _createBlogService.CreateBlog(request, token);
                return Ok(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating blog");
                return BadRequest(new { Message = "Error in creating blog" });
            }
        }

        [HttpGet("/api/Blogs/GetAllTags/{categoryId}")]
        public async Task<IActionResult> GetAllTags(int categoryId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var tags = await _tagsRepository
                    .GetAll()
                    .AsNoTracking()
                    .Where(c => c.Category_Id == categoryId)
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .ToListAsync(token);

                if(tags.Count == 0)
                {
                    return NotFound(new { Message = "No tags found" });
                }

                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving tags");
                return BadRequest(new { Message = "Error in retriving tags" });
            }
        }

        [HttpGet("/api/Blogs/GetAllBlogsByTag/{tagId}")]
        public async Task<IActionResult> GetAllBlogsByTag(int tagId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var blogs = await _blogRepository
                    .GetAll()
                    .AsNoTracking()
                    .Where(c => c.TagId == tagId)
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .ToListAsync(token);

                if(blogs.Count == 0)
                {
                    return NotFound(new { Message = "No blogs found" });
                }

                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving blogs by tagID: {tagId}");
                return BadRequest(new { Message = "Error in retriving blogs" });
            }
        }

        [HttpGet("/api/Blogs/GetAllBlogs")]
        public async Task<IActionResult> GetAllBlogs(PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                paginationDto.Validate();
                var blogs = await _blogRepository
                    .GetAll()
                    .AsNoTracking()
                    .Include(c => c.Tag)
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .ToListAsync(token);

                if(blogs.Count == 0)
                {
                    return NotFound(new { Message = "No blog found" });
                }

                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving all blogs");
                return BadRequest(new { Message = "Error in retriving all blogs" });
            }
        }

    }
}
