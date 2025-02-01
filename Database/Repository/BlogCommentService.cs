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

        public async Task<int> HandleStatusBlogComment(int blogCommentId, int userId, int blogId, CancellationToken token)
        {
            try
            {

                var getBlogComment = await _context.BlogComments.FirstOrDefaultAsync(c => c.BlogId == blogId && c.UserId == userId && c.ID == blogCommentId);
                if(getBlogComment == null)
                {
                    throw new ApplicationException("No blog comment found");
                }
                using var transaction = await _context.Database.BeginTransactionAsync(token);
                var blogCommentLike = await _context.BlogCommentLikes.FirstOrDefaultAsync(c => c.UserId == userId && c.CommentId == blogCommentId);
                if(blogCommentLike == null)
                {
                    var commentLike = new BlogCommentLikes
                    {
                        CommentId = blogCommentId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    await _context.BlogCommentLikes.AddAsync(commentLike, token);
                    getBlogComment.Likes += 1;
                    _context.BlogComments.Update(getBlogComment);
                }
                else
                {
                    _context.BlogCommentLikes.Remove(blogCommentLike);
                    if(getBlogComment.Likes > 0)
                    {
                        getBlogComment.Likes -= 1;
                        _context.BlogComments.Update(getBlogComment);
                    }
                }
                await _context.SaveChangesAsync(token);
                await _context.Database.CommitTransactionAsync(token);
                return blogCommentLike == null ? 1 : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating status of blog comments with blogID: {blogCommentId} and userID: {userId}");
                throw new ApplicationException("Error in updating blog comment status");
            }
        }

        public async Task<(List<BlogCommentDto> blogComments, bool hasMore)> RetrieveBlogComments(int blogId, int userId, bool fullBlogComments, PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                int? commentCount = null;

                if (!fullBlogComments)
                {
                    commentCount = await _context.BlogComments.Where(c => c.BlogId == blogId).CountAsync(token);
                }

                var commentsQuery = await _context.BlogComments
                    .AsNoTracking()
                    .Where(c => c.BlogId == blogId)
                    .Include(c => c.User)
                    //.Include(c => c.Replies)
                    //.Include(c => c.BlogCommentLikes)
                    .Select(c => new BlogCommentDto
                    {
                        CommentId = c.ID,
                        Comment_Content = c.Comment_Content,
                        ParentId = c.ParentId,
                        Likes = c.Likes,
                        IsLiked = c.BlogCommentLikes.Any(bl => bl.CommentId == c.ID && bl.UserId == userId),
                        User = new BlogUserDto
                        {
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            ProfilePicture = c.User.ProfilePictureUrl
                        },
                        Item_Url = c.Item_Url,
                        BlogId = c.BlogId,
                        UserId = c.UserId,
                        createdAt = c.CreatedAt,
                        Replies = new List<BlogCommentDto>()
                    })
                    .OrderByDescending(c => c.createdAt)
                    .ToListAsync();

                var commentLookup = commentsQuery.ToLookup(c => c.ParentId);

                List<BlogCommentDto> BuildHierarky(int? parentId)
                {
                    return fullBlogComments ? commentLookup[parentId]
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
                            UserId = c.UserId,
                            IsLiked = c.IsLiked,
                            Item_Url = c.Item_Url,
                            createdAt = c.createdAt,
                            BlogId = c.BlogId,
                            Replies = BuildHierarky(c.CommentId)
                        })
                        .OrderByDescending(c => c.createdAt)
                        .ToList()
                        : commentLookup[parentId]
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
                            UserId = c.UserId,
                            IsLiked = c.IsLiked,
                            Item_Url = c.Item_Url,
                            createdAt = c.createdAt,
                            BlogId = c.BlogId,
                            Replies = BuildHierarky(c.CommentId)
                        })
                        .OrderByDescending(c => c.createdAt)
                        .Skip(paginationDto.Skip)
                        .Take(paginationDto.Take)
                        .ToList();
                }

                bool hasMore = false;
                if (!fullBlogComments)
                {
                    hasMore = commentLookup.Count == paginationDto.Take && commentLookup.Count < commentCount;
                }

                return (BuildHierarky(null), hasMore);

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
