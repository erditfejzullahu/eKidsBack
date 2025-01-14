using Database.Context;
using Database.DTOs;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        public CreateBlogService(ILogger<CreateBlogService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Blogs> CreateBlog(CreateBlogDto request, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            var blogDto = request.blogDto;
            var tagDto = request.tagDto;
            if(tagDto == null || blogDto == null)
            {
                throw new ApplicationException("Dtos null");
            }
            try
            {
                int? tagId = blogDto?.TagId;
                if (!tagId.HasValue)
                {
                    var tagsAsQueryable = _context.Tags.AsQueryable();
                    //var existingTags = await _context.Tags.Where(c => (c.Name.ToLower() == tagDto.Name.ToLower() || tagDto.Children.Any(child => child.Name.ToLower() == c.Name.ToLower())) && c.Category_Id.Value == tagDto.Category_Id.Value).ToListAsync(token);
                    var childNames = tagDto.Children.Select(child => child.Name.ToLower());
                    if(tagDto.Children.Count > 0)
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
                        if(existingTags.Count == 0)
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
                            if(existingParent == null)
                            {
                                if(existingChilds.Select(c => c.Name.ToLower()).Contains(tagDto.Name))
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
                                if(!existingChilds.Any(c => c.Name.ToLower() == child.Name.ToLower()))
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
                            if(existingParent == null)
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

                var blog = new Blogs
                {
                    Title = blogDto.Title,
                    Content = blogDto.Content,
                    CategoryId = blogDto.CategoryId,
                    Status = blogDto.Status,
                    UserId = blogDto.UserId,
                    TagId = tagId.Value,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _context.Blogs.AddAsync(blog, token);
                await _context.SaveChangesAsync(token);

                await transaction.CommitAsync(token);
                return blog;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token); //nuk bon add ja hiq

                _logger.LogError(ex, "Error in creating blog");
                throw new ApplicationException("Failed to create blog.", ex);
            }
        }

        public async Task<List<BlogRetrieveDto>> AllBlogRetrieve(int userId, PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var blogs = await _context.Blogs
                    .AsNoTracking()
                    .Include(c => c.Tag)
                    .ThenInclude(c => c.Children)
                    .Include(c => c.User)
                    .Include(c => c.BlogLikes)
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .Select(c => new BlogRetrieveDto
                    {
                        Title = c.Title,
                        CategoryId = c.CategoryId,
                        UserId = c.UserId,
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
                        Status = c.Status,
                        IsLiked = c.BlogLikes.Any(bl => bl.UserId == userId),
                        ImageUrls = c.ImageUrls,
                        CreatedAt = c.CreatedAt,
                    })
                    .ToListAsync(token);
                return blogs;
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
                    await _context.SaveChangesAsync(token);
                    return 1; // 1 for adding like 0 for removing like
                }
                else
                {
                    _context.Remove(blogLikeStatus);
                    await _context.SaveChangesAsync(token);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating status of like with blogID: {blogId} and userID: {userId}");
                throw new ApplicationException("Error in updating status of like");
            }
        }
    }
}
