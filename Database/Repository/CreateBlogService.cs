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

        public async Task<Blogs> CreateBlog(CreateBlogDto request, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            var blogDto = request.blogDto;
            var tagDto = request.tagDto;
            string? blogImageUrl = null;

            if(request.blogDto.Images != null && request.blogDto.Images.Count != 0)
            {
                try
                {
                    var imageUrls = new List<string>();
                    var requestUrl = _httpContextAccessor.HttpContext?.Request;
                    foreach (var image in request.blogDto.Images)
                    {
                        var imageUrl = await _fileUpload.UploadFile(image, FileCategory.Other);
                        var fullUrl = $"{requestUrl.Scheme}://{requestUrl.Host}{imageUrl}";
                        imageUrls.Add(fullUrl);
                    }

                    blogImageUrl = JsonConvert.SerializeObject(imageUrls);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " Error in uploading blog image");
                    throw new ApplicationException("Error in uploading blog image", ex);
                }
            }

            if(blogDto == null)
            {
                throw new ApplicationException("Dtos null");
            }
            try
            {
                int? tagId = blogDto?.TagId;
                if(tagDto != null)
                {
                    if (!tagId.HasValue)
                    {
                        var tagsAsQueryable = _context.Tags.AsQueryable();
                        //var existingTags = await _context.Tags.Where(c => (c.Name.ToLower() == tagDto.Name.ToLower() || tagDto.Children.Any(child => child.Name.ToLower() == c.Name.ToLower())) && c.Category_Id.Value == tagDto.Category_Id.Value).ToListAsync(token);
                        var childNames = tagDto.Children.Select(child => child.Name.ToLower());
                        if (tagDto.Children.Count > 0)
                        {
                            tagsAsQueryable = tagsAsQueryable.Where(c => (c.Name.ToLower() == tagDto.Name.ToLower() || childNames.Contains(c.Name.ToLower())) && c.Category_Id == tagDto.Category_Id);
                        }
                        else
                        {
                            tagsAsQueryable = tagsAsQueryable.Where(c => c.Name.ToLower() == tagDto.Name.ToLower() && c.Category_Id.Value == tagDto.Category_Id.Value);
                        }
                        var existingTags = await tagsAsQueryable.ToListAsync(token);
                        var existingParent = existingTags.FirstOrDefault(c => c.Parent_Id == null);
                        var existingChilds = existingTags.Where(c => c.Parent_Id != null);
                        //_logger.LogError($"ExistingChilds: {existingParent.Name}");
                        if (tagDto.Children.Count > 0)
                        {
                            if (existingTags.Count == 0)
                            {
                                var createTag = new Tags
                                {
                                    Name = tagDto.Name,
                                    Category_Id = tagDto.Category_Id,
                                    Parent_Id = null,
                                    CreatedAt = DateTime.UtcNow,
                                    LastModified = DateTime.UtcNow
                                };
                                await _context.AddAsync(createTag, token);
                                await _context.SaveChangesAsync(token);

                                tagId = createTag.ID;

                                foreach (var child in tagDto.Children)
                                {
                                    if (!existingChilds.Any(c => c.Name.ToLower() == child.Name.ToLower()))
                                    {
                                        var childs = new Tags
                                        {
                                            Name = child.Name,
                                            Category_Id = child.Category_Id,
                                            Parent_Id = tagId,
                                            CreatedAt = DateTime.UtcNow,
                                            LastModified = DateTime.UtcNow
                                        };
                                        await _context.Tags.AddAsync(childs, token);
                                    }
                                }
                                await _context.SaveChangesAsync(token);
                            }
                            else
                            {
                                if (existingParent == null)
                                {
                                    if (existingChilds.Select(c => c.Name.ToLower()).Contains(tagDto.Name))
                                    {
                                        tagId = existingChilds?.FirstOrDefault(c => c.Name.ToLower() == tagDto.Name.ToLower())?.Parent_Id;
                                    }
                                    else
                                    {
                                        var parent = new Tags
                                        {
                                            Name = tagDto.Name,
                                            Category_Id = tagDto.Category_Id.Value,
                                            CreatedAt = DateTime.UtcNow,
                                            LastModified = DateTime.UtcNow
                                        };
                                        await _context.Tags.AddAsync(parent, token);
                                        await _context.SaveChangesAsync(token);
                                        tagId = parent.ID;
                                    }
                                }
                                else
                                {
                                    tagId = existingParent.ID;
                                }

                                foreach (var child in tagDto.Children)
                                {
                                    if (!existingChilds.Any(c => c.Name.ToLower() == child.Name.ToLower()))
                                    {
                                        var childs = new Tags
                                        {
                                            Name = child.Name,
                                            Category_Id = child.Category_Id,
                                            Parent_Id = tagId,
                                            CreatedAt = DateTime.UtcNow,
                                            LastModified = DateTime.UtcNow
                                        };
                                        await _context.Tags.AddAsync(childs, token);
                                        await _context.SaveChangesAsync(token);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (existingTags.Count == 0)
                            {
                                var createTag = new Tags
                                {
                                    Name = tagDto.Name,
                                    Category_Id = tagDto.Category_Id,
                                    Parent_Id = null,
                                    CreatedAt = DateTime.UtcNow,
                                    LastModified = DateTime.UtcNow
                                };
                                await _context.Tags.AddAsync(createTag, token);
                                await _context.SaveChangesAsync(token);

                                tagId = createTag.ID;
                            }
                            else
                            {
                                if (existingParent == null)
                                {
                                    if (existingChilds.Select(c => c.Name.ToLower()).Contains(tagDto.Name.ToLower()))
                                    {
                                        tagId = existingChilds?.FirstOrDefault(c => c.Name.ToLower() == tagDto.Name.ToLower())?.Parent_Id;
                                    }
                                    else
                                    {
                                        var parent = new Tags
                                        {
                                            Name = tagDto.Name,
                                            Category_Id = tagDto.Category_Id,
                                            Parent_Id = null,
                                            CreatedAt = DateTime.UtcNow,
                                            LastModified = DateTime.UtcNow
                                        };
                                        await _context.Tags.AddAsync(parent, token);
                                        await _context.SaveChangesAsync(token);
                                    }
                                }
                                else
                                {
                                    tagId = existingParent.ID;
                                }
                            }
                        }

                    }
                }

                string generateAiMessage = $"Me pergatit nje pershkrim nga kjo permbajtje e ketij blogu dhe ne pershkrimin e tij fillo me 'Mesa duket ky blog ka te beje me'. Permbajtja eshte kjo: {blogDto.Content}";
                var blog = new Blogs
                {
                    Title = blogDto.Title,
                    Content = blogDto.Content,
                    CategoryId = blogDto.CategoryId,
                    Status = blogDto.Status,
                    UserId = blogDto.UserId,
                    TagId = tagId.Value,
                    ImageUrls = blogImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _context.Blogs.AddAsync(blog, token);
                await _context.SaveChangesAsync(token);

                await transaction.CommitAsync(token);
                var aiMessageToGenerate = new AIMessageGenerationDto
                {
                    Id = blog.ID,
                    Message = generateAiMessage
                };

                _rabbitMqService.SendMessage(aiMessageToGenerate);
                return blog;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token); //nuk bon add ja hiq

                _logger.LogError(ex, "Error in creating blog");
                throw new ApplicationException("Failed to create blog.", ex);
            }
        }

        public async Task<BlogRetrieveDto> GetBlogById(int blogId, int userId, CancellationToken token)
        {
            try
            {
                var blog = await _context.Blogs
                    .AsNoTracking()
                    .Where(c => c.ID == blogId)
                    .Include(c => c.Tag)
                    .ThenInclude(c => c.Children)
                    .Include(c => c.BlogLikes)
                    .Select(c => new BlogRetrieveDto
                    {
                        ID = c.ID,
                        Title = c.Title,
                        CategoryId = c.CategoryId,
                        UserId = c.UserId,
                        CommentsCount = c.BlogComments.Count,
                        Tags = new BlogRetrieveTagDto
                        {
                            Name = c.Tag.Name,
                            TagId = c.Tag.ID,
                            Children = c.Tag.Children.Select(t => new BlogRetrieveTagDto
                            {
                                Name = t.Name,
                                TagId = t.ID
                            }).ToList() ?? new List<BlogRetrieveTagDto>(),
                        },
                        User = new BlogRetrieveUserDto
                        {
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            ProfilePicture = c.User.ProfilePictureUrl
                        },
                        TagId = c.TagId,
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
                    .Include(c => c.Tag)
                    .ThenInclude(c => c.Children)
                    .Include(c => c.User)
                    .Include(c => c.BlogLikes)
                    .Where(c => c.TagId == tagId || c.Tag.Children.Any(c => c.ID == tagId))
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
                        Tags = new BlogRetrieveTagDto
                        {
                            Name = c.Tag.Name,
                            TagId = c.Tag.ID,
                            Children = c.Tag.Children.Select(t => new BlogRetrieveTagDto
                            {
                                Name = t.Name,
                                TagId = t.ID
                            }).ToList() ?? new List<BlogRetrieveTagDto>()
                        },
                        TagId = c.TagId,
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
                var query = _context.Blogs.Include(c => c.User).ThenInclude(c => c.Friends).OrderByDescending(c => c.CreatedAt).AsNoTracking();

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
                        Tags = new BlogRetrieveTagDto
                        {
                            Name = c.Tag.Name,
                            TagId = c.Tag.ID,
                            Children = c.Tag.Children.Where(t => t.Parent_Id == c.Tag.ID).Select(t => new BlogRetrieveTagDto
                            {
                                Name = t.Name,
                                TagId = t.ID
                            }).ToList() ?? new List<BlogRetrieveTagDto>()
                        },
                        TagId = c.TagId,
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
