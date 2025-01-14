using Database.Context;
using Database.DTOs;
using Database.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Database.Repository
{
    public class BlogCommentService : IBlogCommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BlogCommentService> _logger;
        private readonly IFileUploadService _fileUploadService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BlogCommentService(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context, 
            ILogger<BlogCommentService> logger,
            IFileUploadService fileUploadService
            )
        {
            _httpContextAccessor = httpContextAccessor;
            _fileUploadService = fileUploadService;
            _context = context;
            _logger = logger;
        }

        public async Task<List<BlogCommentDto>> RetrieveBlogComments(int blogId, CancellationToken token)
        {
            try
            {
                var comments = await _context.BlogComments
                    .Where(c => c.BlogId == blogId)
                    .Include(c => c.User)
                    .Include(c => c.Replies)
                    .Select(c => new BlogCommentDto
                    {
                        CommentId = c.ID,
                        Comment_Content = c.Comment_Content,
                        ParentId = c.ParentId,
                        Likes = c.Likes,
                        User = new BlogUserDto
                        {
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            ProfilePicture = c.User.ProfilePictureUrl
                        },
                        Item_Url = c.Item_Url,
                        BlogId = c.BlogId,
                        createdAt = c.CreatedAt,
                        Replies = new List<BlogCommentDto>()
                    })
                    .OrderByDescending(c => c.createdAt)
                    .AsNoTracking()
                    .ToListAsync(token);

                var commentLookup = comments.ToLookup(c => c.ParentId);

                List<BlogCommentDto> BuildHierarky(int? parentId)
                {
                    return commentLookup[parentId]
                        .Select(c => new BlogCommentDto
                        {
                            CommentId = c.CommentId,
                            Comment_Content = c.Comment_Content,
                            ParentId = c.ParentId,
                            User = new BlogUserDto
                            {
                                Name = c.User.Name,
                                ProfilePicture = c.User.ProfilePicture
                            },
                            Likes = c.Likes,
                            createdAt = c.createdAt,
                            BlogId = c.BlogId,
                            Replies = BuildHierarky(c.CommentId)
                        })
                        .OrderByDescending(c => c.createdAt)
                        .ToList();
                }
                return BuildHierarky(null);

                //return comments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving blog comments");
                throw new ApplicationException("Error in retriving blog comments");
            }
        }

        public async Task<BlogComments> CreateBlogComment(CreateBlogComment blogDto, CancellationToken token)
        {
            try
            {
                string? itemUrl = null;
                var request = _httpContextAccessor.HttpContext?.Request;
                if (!string.IsNullOrEmpty(blogDto.base64Data))
                {
                    try
                    {
                        string relativeUrl = await _fileUploadService.UploadFile(blogDto.base64Data, FileCategory.Uploads);
                        itemUrl = $"{request.Scheme}://{request.Host}{relativeUrl}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in upliading file");
                        throw new ApplicationException("Error in  uploading file");
                    }
                }
                var comment = new BlogComments
                {
                    UserId = blogDto.UserId,
                    BlogId = blogDto.BlogId,
                    Item_Url = itemUrl,
                    Likes = 0,
                    Comment_Content = blogDto.Comment_Content,
                    ParentId = blogDto.ParentId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.BlogComments.AddAsync(comment, token);
                await _context.SaveChangesAsync(token);
                return comment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inc reating comment");
                throw new ApplicationException("Error in creating comment");
            }
        }
    }
}
