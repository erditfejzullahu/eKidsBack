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
        private readonly IBlogCommentService _blogCommentService;

        public BlogController(
            ILogger<BlogController> logger,
            IRepository<Tags> tagsRepository,
            ICreateBlogService createBlogService,
            IRepository<Blogs> blogRepository,
            IBlogCommentService blogCommentService)
        {
            _logger = logger;
            _blogRepository = blogRepository;
            _createBlogService = createBlogService;
            _tagsRepository = tagsRepository;
            _blogCommentService = blogCommentService;
        }

        [HttpPost("/api/Blogs/CreateComment")]
        public async Task<IActionResult> CreateBlogComment([FromBody] CreateBlogComment blogComment, CancellationToken token)
        {
            try
            {
                var comment = await _blogCommentService.CreateBlogComment(blogComment, token);
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating blog comment");
                return BadRequest(new { Message = "Error in creating blog comment" });
            }
        }

        [HttpPost("/api/Blogs/LikeComment")]
        public async Task<IActionResult> LikeBlogComment([FromQuery] int blogCommentId, [FromQuery] int userId, [FromQuery] int blogId, CancellationToken token)
        {
            try
            {
                int likeStatus = await _blogCommentService.HandleStatusBlogComment(blogCommentId, userId, blogId, token);
                if(likeStatus == 0)
                {
                    return Ok(new { Message = "LikeRemove" });
                }
                else if(likeStatus == 1)
                {
                    return Ok(new { Message = "LikeAdd" });
                }

                return BadRequest(new { Message = "Unexpected like status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in changing status of like in comment with blogId: {blogCommentId} and user: {userId}");
                return BadRequest(new { Message = "Error in changing status of blog comment like" });
            }
        }

        [HttpPost("/api/Blogs/LikeBlog")]
        public async Task<IActionResult> LikeBlog([FromQuery] int blogId, [FromQuery] int userId, CancellationToken token)
        {
            try
            {
                int likeStatus = await _createBlogService.HandleStatusBlogLike(blogId, userId, token);
                _logger.LogError(likeStatus, " statusi");
                if (likeStatus == 0)
                {
                    return Ok(new { Message = "LikeRemove" });
                }
                else if(likeStatus == 1)
                {
                    return Ok(new { Message = "LikeAdd" });
                }
                return BadRequest(new { Message = "Unexpected like status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating like status for blogID: {blogId} and userID: {userId}");
                return BadRequest(new { Message = "Error in updating like status" });
            }
        }

        [HttpGet("/api/Blogs/GetCommentsByBlog/{blogId}/{userId}")]
        public async Task<IActionResult> GetBlogComments(int blogId, int userId, CancellationToken token)
        {
            try
            {
                var comments = await _blogCommentService.RetrieveBlogComments(blogId, userId, token);
                if(comments.Count == 0)
                {
                    return NotFound(new { Message = "No comments are made" });
                }
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving blog comments for blog id: {blogId}");
                return BadRequest(new { Message = "Error in retriving blog comments" });
            }
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

        [HttpGet("/api/Blogs/GetAllTagsWithChild/")]
        public async Task<IActionResult> GetAllTags([FromQuery] int categoryId, CancellationToken token)
        {
            try
            {
                var tags = await _tagsRepository
                    .GetAll()
                    .Where(c => c.Parent_Id == null && c.Category_Id == categoryId)
                    .Include(c => c.Children)
                    .AsNoTracking()
                    .Select(c => new
                    {
                        c.ID,
                        c.Name,
                        c.Parent_Id,
                        Children = c.Children.Select(t => new
                        {
                            t.ID,
                            t.Name,
                            t.Parent_Id
                        }).ToList()
                    })
                    //.Skip(paginationDto.Skip)
                    //.Take(paginationDto.Take)
                    .ToListAsync(token);

                if(tags.Count == 0)
                {
                    return NotFound(new { Message = "No tags found" });
                }

                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving tag data");
                return BadRequest(new { Message = "Error in retriving data" });
            }
        }

        [HttpGet("/api/Blogs/GetAllTags/{categoryId}")]
        public async Task<IActionResult> GetAllParentTags(int categoryId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var tags = await _tagsRepository
                    .GetAll()
                    .AsNoTracking()
                    .Where(c => c.Category_Id == categoryId && c.Parent_Id == null)
                    .Select(c => new
                    {
                        c.ID,
                        c.Name,
                        Children = c.Children.Select(t => new
                        {
                            t.ID,
                            t.Name
                        })
                    })
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

        [HttpGet("/api/Blogs/GetAllBlogsByTag/{userId}/{tagId}")]
        public async Task<IActionResult> GetAllBlogsByTag(int tagId, int userId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var blogs = await _createBlogService.AllBlogByTagRetrieve(userId, tagId, paginationDto, token);

                if(blogs.blogs.Count == 0)
                {
                    return NotFound(new { Message = "No blogs found" });
                }

                return Ok(new {data = blogs.blogs, blogs.hasMore});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving blogs by tagID: {tagId}");
                return BadRequest(new { Message = "Error in retriving blogs" });
            }
        }

        [HttpGet("/api/Blogs/GetAllBlogs/{userId}")]
        public async Task<IActionResult> GetAllBlogs(int userId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                paginationDto.Validate();
                var blogs = await _createBlogService.AllBlogRetrieve(userId, paginationDto, token);

                if(blogs.blogs.Count == 0)
                {
                    return NotFound(new { Message = "No blog found" });
                }

                return Ok(new { data = blogs.blogs, blogs.hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving all blogs");
                return BadRequest(new { Message = "Error in retriving all blogs" });
            }
        }

    }
}
