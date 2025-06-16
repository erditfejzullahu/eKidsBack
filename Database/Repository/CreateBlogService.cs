using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ganss.Xss;


namespace Database.Repository
{
    public class CreateBlogService : ICreateBlogService
    {
        private readonly ILogger<CreateBlogService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUpload;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RabbitMqService _rabbitMqService;

        public CreateBlogService(RabbitMqService rabbitMqService, ILogger<CreateBlogService> logger, IHttpContextAccessor httpContextAccessor, ApplicationDbContext context, IFileUploadService fileUpload)
        {
            _logger = logger;
            _context = context;
            _fileUpload = fileUpload;
            _httpContextAccessor = httpContextAccessor;
            _rabbitMqService = rabbitMqService;
        }

        public async Task<Blogs> CreateBlog(CreateBlogDto blogDto, CancellationToken token)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                if(blogDto == null)
                {
                    throw new ApplicationException("Blog dto is null");
                }

                if (string.IsNullOrWhiteSpace(blogDto.Title))
                    throw new ArgumentException("Blog title is required", nameof(blogDto.Title));

                if (string.IsNullOrWhiteSpace(blogDto.Content))
                    throw new ArgumentException("Blog content is required", nameof(blogDto.Content));

                string? blogImageUrl = string.Empty;
                if(blogDto.Images != null && blogDto.Images.Count != 0)
                {
                    try
                    {
                        var imageUrls = new List<string>();
                        var requestUrl = _httpContextAccessor.HttpContext?.Request;
                        foreach (var image in blogDto.Images)
                        {
                            var imageUrl = await _fileUpload.UploadFile(image, FileCategory.Other);
                            var fullUrl = $"{requestUrl.Scheme}://{requestUrl.Host}{imageUrl}";
                            imageUrls.Add(fullUrl);
                        }
                        blogImageUrl = JsonConvert.SerializeObject(imageUrls);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in uploading image");
                        throw new ApplicationException("Error uploading blog images");
                    }
                }

                var sanitizer = new HtmlSanitizer();

                var blog = new Blogs
                {
                    Title = sanitizer.Sanitize(blogDto.Title.Trim()),
                    Content = sanitizer.Sanitize(blogDto.Content.Trim()),
                    CategoryId = blogDto.CategoryId,
                    Status = blogDto.Status,
                    UserId = blogDto.UserId,
                    ImageUrls = blogImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Blogs.AddAsync(blog);
                await _context.SaveChangesAsync(token);

                var blogTagsDto = blogDto.Tags.Where(tag => tag != null && !string.IsNullOrWhiteSpace(tag.Name))
                    .Select(tag => new TagsDto
                    {
                        Name = sanitizer.Sanitize(tag.Name.Trim())
                    })
                    .DistinctBy(tag => tag.Name.ToLower());

                var newTagList = new List<Tags>();
                var existingTagList = new List<Tags>();
                foreach (var tag in blogTagsDto)
                {
                    var existingTag = await _context.Tags.Where(c => c.Name.ToLower() == tag.Name.ToLower()).FirstOrDefaultAsync(token);
                    if(existingTag == null)
                    {
                        newTagList.Add(new Tags
                        {
                            Name = tag.Name,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow,
                        });
                    }
                    else
                    {
                        existingTagList.Add(existingTag);
                    }
                }
                if(newTagList.Count > 0)
                {
                    await _context.Tags.AddRangeAsync(newTagList, token);
                    await _context.SaveChangesAsync(token);
                }

                var blogsWithTagsList = new List<BlogsWithTags>();
                foreach (var tagItem in newTagList)
                {
                    blogsWithTagsList.Add(new BlogsWithTags
                    {
                        BlogId = blog.ID,
                        TagId = tagItem.ID,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    });
                }

                if(existingTagList.Count > 0)
                {
                    foreach (var tagItem in existingTagList)
                    {
                        blogsWithTagsList.Add(new BlogsWithTags
                        {
                            BlogId = blog.ID,
                            TagId = tagItem.ID,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });
                    }
                }

                if(blogsWithTagsList.Count > 0)
                {
                    await _context.BlogsWithTags.AddRangeAsync(blogsWithTagsList, token);
                }
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return blog;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                throw new ApplicationException(ex.Message);
            }
        }

       
        public async Task<BlogRetrieveDto> GetBlogById(int blogId, int userId, CancellationToken token)
        {
            try
            {
                var blog = await _context.Blogs
                    .AsNoTracking()
                    .Where(c => c.ID == blogId)
                    .Select(c => new BlogRetrieveDto
                    {
                        ID = c.ID,
                        Title = c.Title,
                        CategoryId = c.CategoryId,
                        UserId = c.UserId,
                        CommentsCount = c.BlogComments.Count,
                        Tags = c.BlogTags.Select(c => new BlogRetrieveTagDto
                        {
                            Name = c.Tag.Name,
                            TagId = c.Tag.ID,
                        }).ToList(),
                        User = new BlogRetrieveUserDto
                        {
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            ProfilePicture = c.User.ProfilePictureUrl
                        },
                        Content = c.Content,
                        Likes = c.Likes,
                        Status = c.Status,
                        IsLiked = c.BlogLikes.Any(bl => bl.UserId == userId),
                        ImageUrls = c.ImageUrls,
                        CreatedAt = c.CreatedAt,
                    })
                    .FirstOrDefaultAsync(token);
                return blog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in getting post with id {blogId}");
                throw new ApplicationException("Error in getting blog post");
            }
        }
        public async Task<(List<BlogRetrieveDto> blogs, bool hasMore)> AllBlogByTagRetrieve(int userId, int tagId, PaginationDto paginationDto, CancellationToken token, GetFriendBlogsOrAll getFriendBlogsOrAll)
        {
            try
            {
                var query = _context.Blogs
                    .Where(c => c.BlogTags.Any(c => c.Tag.ID == tagId))
                    .AsNoTracking();

                if(getFriendBlogsOrAll == GetFriendBlogsOrAll.All)
                {
                    query = query.Where(c => c.Status == BlogStatus.Public);
                }
                else
                {
                    query = query.Where(c =>
                        c.Status == BlogStatus.Public && c.User.Friends.Any(uf => uf.FriendId == userId)
                        ||
                        c.Status == BlogStatus.FriendOnly && c.User.Friends.Any(uf => uf.FriendId == userId));
                }

                var blogsCount = await query.CountAsync();

                var blogs = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .Select(c => new BlogRetrieveDto
                    {
                        ID = c.ID,
                        Title = c.Title,
                        CategoryId = c.CategoryId,
                        UserId = c.UserId,
                        CommentsCount = c.BlogComments.Count,
                        User = new BlogRetrieveUserDto
                        {
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            ProfilePicture = c.User.ProfilePictureUrl
                        },
                        Tags = c.BlogTags.Select(b => new BlogRetrieveTagDto
                        {
                            Name = b.Tag.Name,
                            TagId = b.Tag.ID,
                        }).ToList(),
                        Content = c.Content,
                        Likes = c.Likes,
                        Status = c.Status,
                        IsLiked = c.BlogLikes.Any(bl => bl.UserId == userId),
                        ImageUrls = c.ImageUrls,
                        CreatedAt = c.CreatedAt,
                    })
                    .ToListAsync(token);
                bool hasMore = blogs.Count == paginationDto.Take && blogs.Count < blogsCount;
                return (blogs, hasMore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving blog for userid: {userId} and tagId: {tagId}");
                throw new ApplicationException("Error in retriving blogs by tags");
            }
        }
        public async Task<(List<BlogRetrieveDto> blogs, bool hasMore)> AllBlogRetrieve(int userId, PaginationDto paginationDto, CancellationToken token, BlogDiscussionRetrivalType retrivalType, GetFriendBlogsOrAll getFriendBlogsOrAll)
        {
            try
            {
                var query = _context.Blogs.OrderByDescending(c => c.CreatedAt).AsNoTracking();

                if (retrivalType == BlogDiscussionRetrivalType.ProfileSection)
                {
                    query = query.Where(c => c.UserId == userId);
                }
                else
                {
                    if(getFriendBlogsOrAll == GetFriendBlogsOrAll.All)
                    {
                        query = query.Where(c => c.Status == BlogStatus.Public);
                    }
                    else
                    {
                        query = query.Where(c =>
                            c.Status == BlogStatus.FriendOnly && c.User.Friends.Any(uf => uf.FriendId == userId)
                            ||
                            c.Status == BlogStatus.Public && c.User.Friends.Any(uf => uf.FriendId == userId));
                    }
                }

                var blogsCount = await query.CountAsync(token);

                var blogs = await query
                    .Select(c => new BlogRetrieveDto
                    {
                        ID = c.ID,
                        Title = c.Title,
                        CategoryId = c.CategoryId,
                        UserId = c.UserId,
                        CommentsCount = c.BlogComments.Count,
                        User = new BlogRetrieveUserDto
                        {
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            ProfilePicture = c.User.ProfilePictureUrl
                        },
                        Tags = c.BlogTags.Select(b => new BlogRetrieveTagDto
                        {
                            Name = b.Tag.Name,
                            TagId = b.Tag.ID,
                        }).ToList(),
                        Content = c.Content,
                        Likes = c.Likes,
                        Status = c.Status,
                        IsLiked = c.BlogLikes.Any(bl => bl.BlogId == c.ID && bl.UserId == userId),
                        ImageUrls = c.ImageUrls,
                        CreatedAt = c.CreatedAt,
                    })
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .ToListAsync(token);
                bool hasMore = blogs.Count == paginationDto.Take && blogs.Count < blogsCount;
                return (blogs, hasMore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving blogs including userID: {userId}");
                throw new ApplicationException("Error retriving blogs");
            }
        }

        public async Task<int> HandleStatusBlogLike(int blogId, int userId, CancellationToken token)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync(token);

                var addBlogLike = await _context.Blogs.FirstOrDefaultAsync(c => c.ID == blogId);
                if (addBlogLike == null)
                {
                    throw new ArgumentNullException(nameof(addBlogLike), $"Blog with ID {blogId} not found.");
                }
                var blogLikeStatus = await _context.BlogLikes.FirstOrDefaultAsync(c => c.UserId == userId && c.BlogId == blogId);
                if (blogLikeStatus == null)
                {
                    var blogLike = new BlogLikes
                    {
                        BlogId = blogId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    await _context.BlogLikes.AddAsync(blogLike, token);
                    addBlogLike.Likes += 1;
                    _context.Blogs.Update(addBlogLike);
                    _logger.LogError("its updated");
                }
                else
                {
                    _context.BlogLikes.Remove(blogLikeStatus);
                    if(addBlogLike.Likes > 0)
                    {
                        addBlogLike.Likes -= 1;
                    }
                    _context.Blogs.Update(addBlogLike);
                }
                _logger.LogError("here");
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return blogLikeStatus == null ? 1 : 0;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating status of like with blogID: {blogId} and userID: {userId}");
                throw new ApplicationException("Error in updating status of like");
            }
        }
    }
}
