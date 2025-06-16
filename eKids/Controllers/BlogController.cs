using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Database.Shared.Enums;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography.Xml;

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
        private readonly ApplicationDbContext _context;

        public BlogController(
            ApplicationDbContext context,
            ILogger<BlogController> logger,
            IRepository<Tags> tagsRepository,
            ICreateBlogService createBlogService,
            IRepository<Blogs> blogRepository,
            IBlogCommentService blogCommentService)
        {
            _context = context;
            _logger = logger;
            _blogRepository = blogRepository;
            _createBlogService = createBlogService;
            _tagsRepository = tagsRepository;
            _blogCommentService = blogCommentService;
        }

        [Authorize]
        [HttpGet("/api/Blogs/GetBlogById/{blogId}/{userId}")]
        public async Task<IActionResult> GetBlogById(int blogId, int userId, CancellationToken token)
        {
            try
            {
                var blog = await _createBlogService.GetBlogById(blogId, userId, token);
                if(blog == null)
                {
                    return NotFound("No blogs found");
                }
                return Ok(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving blog id");
                return BadRequest(new { Message = "Error in retriving blog" });
            }
        }

        [Authorize]
        [HttpGet("/api/Blogs/GetByName")]
        public async Task<IActionResult> GetBlogByName([FromQuery] string title, CancellationToken token)
        {
            try
            {
                var blogs = await _context.Blogs
                    .Where(c => EF.Functions.Contains(c.Title, $"\"{title}*\""))
                    .Select(c => new
                    {
                        c.ID,
                        c.Title,
                        c.CategoryId,
                        BlogTags = c.BlogTags.Select(bt => bt.Tag.Name).ToList(),
                        c.CreatedAt,
                        User = c.User != null ? new
                        {
                            c.User.Username,
                            c.User.ProfilePictureUrl,
                            c.User.ID
                        } : null
                    })
                    .ToListAsync(token);

                if(blogs.Count == 0)
                {
                    return NotFound(new { Message = "No blogs found" });
                }
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retriving blogs by name");
                return BadRequest(new { Message = "Error retriving blogs by name" });
            }
        }

        [Authorize]
        [HttpPost("/api/Blogs/CreateComment")]
        public async Task<IActionResult> CreateBlogComment([FromBody] CreateBlogComment blogComment, CancellationToken token)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Model invalid");
                }
                var comment = await _blogCommentService.CreateBlogComment(blogComment, token);
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating blog comment");
                return BadRequest(new { Message = "Error in creating blog comment" });
            }
        }

        [Authorize]
        [HttpPost("/api/Blogs/LikeComment")]
        public async Task<IActionResult> LikeBlogComment([FromQuery] int blogCommentId, [FromQuery] int blogId, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

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
                _logger.LogError(ex, $"Error in changing status of like in comment with blogId: {blogCommentId}");
                return BadRequest(new { Message = "Error in changing status of blog comment like" });
            }
        }

        [Authorize]
        [HttpPost("/api/Blogs/LikeBlog")]
        public async Task<IActionResult> LikeBlog([FromQuery] int blogId, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                int likeStatus = await _createBlogService.HandleStatusBlogLike(blogId, userId, token);
                
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
                _logger.LogError(ex, $"Error in updating like status for blogID: {blogId}");
                return BadRequest(new { Message = "Error in updating like status" });
            }
        }

        [HttpGet("/api/Blogs/GetCommentsByBlog/{blogId}")]
        public async Task<IActionResult> GetBlogComments(int blogId, [FromQuery] bool fullBlogComments, [FromQuery] PaginationDto paginationDto,  CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var comments = await _blogCommentService.RetrieveBlogComments(blogId, userId, fullBlogComments, paginationDto, token);
                if(comments.blogComments.Count == 0)
                {
                    return NotFound(new { Message = "No comments are made" });
                }
                return Ok(new {comments.blogComments, comments.hasMore});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving blog comments for blog id: {blogId}");
                return BadRequest(new { Message = "Error in retriving blog comments" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromBody] CreateBlogDto request, CancellationToken token)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Model invalid" });
                }

                var blog = await _createBlogService.CreateBlog(request, token);
                return Ok(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating blog");
                return BadRequest(new { Message = "Error in creating blog" });
            }
        }

        [Authorize]
        [HttpGet("/api/Blogs/GetAllBlogTags/")]
        public async Task<IActionResult> GetAllTags(string? searchParam, CancellationToken token)
        {
            try
            {
                var query = _context.Tags.AsNoTracking();
                if (!string.IsNullOrEmpty(searchParam))
                {
                    query = query.Where(c => EF.Functions.Contains(c.Name, $"\"{searchParam}*\""));
                }

                var tags = await query
                    .Select(c => new
                    {
                        c.ID,
                        c.Name,
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

        [Authorize]
        [HttpGet("/api/Blogs/GetAllTags/{categoryId}")]
        public async Task<IActionResult> GetAllParentTags(int categoryId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var tags = await _context.BlogsWithTags
                    .Where(c => c.Blog.CategoryId == categoryId)
                    .Select(c => new
                    {
                        c.Tag.ID,
                        c.Tag.Name
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

        [HttpGet("/api/Blogs/GetAllBlogsByTag/{tagId}")]
        public async Task<IActionResult> GetAllBlogsByTag(int tagId, [FromQuery] PaginationDto paginationDto, [FromQuery] GetFriendBlogsOrAll friendsBlogsOrAll, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var blogs = await _createBlogService.AllBlogByTagRetrieve(userId, tagId, paginationDto, token, friendsBlogsOrAll);

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

        [HttpGet("/api/Blogs/GetAllBlogsByUser/{userId}")]
        public async Task<IActionResult> GetAllBlogsByUser(int userId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                paginationDto.Validate();
                var blogs = await _createBlogService.AllBlogRetrieve(userId, paginationDto, token, BlogDiscussionRetrivalType.ProfileSection, GetFriendBlogsOrAll.All);
                if(blogs.blogs.Count == 0)
                {
                    return NotFound(new { Message = "No blogs found" });
                }
                return Ok(new { data = blogs.blogs, blogs.hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " error in getting all blogs by user given");
                return BadRequest();
            }
        }

        [HttpGet("/api/Blogs/GetAllBlogs/")]
        public async Task<IActionResult> GetAllBlogs([FromQuery] PaginationDto paginationDto, [FromQuery] GetFriendBlogsOrAll friendsBlogsOrAll, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                paginationDto.Validate();
                var blogs = await _createBlogService.AllBlogRetrieve(userId, paginationDto, token, BlogDiscussionRetrivalType.AllSection, friendsBlogsOrAll);

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

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlog(int id, CancellationToken token)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                var blog = await _context.Blogs.Where(c => c.ID == id).FirstOrDefaultAsync(token);
                if(blog == null)
                {
                    return NotFound();
                }
                if(blog.UserId != userId)
                {
                    return Forbid();
                }
                var conversations = await _context.Conversations.Where(c => c.BlogId == id).ToListAsync(token);
                var blogComments = await _context.BlogComments.Where(c => c.BlogId == id).Include(c => c.BlogCommentLikes).ToListAsync(token);
                var blogLikes = await _context.BlogLikes.Where(c => c.BlogId == id).ToListAsync(token);
                if (conversations.Count > 0) _context.Conversations.RemoveRange(conversations);
                if (blogComments.Count > 0) _context.BlogComments.RemoveRange(blogComments);
                if (blogLikes.Count > 0) _context.BlogLikes.RemoveRange(blogLikes);
                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error deleting blog");
                return BadRequest();
            }
        }

    }
}
