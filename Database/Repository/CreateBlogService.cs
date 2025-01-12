using Database.Context;
using Database.DTOs;
using Database.Models;
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
            try
            {
                int? tagId = blogDto.TagId;
                if (!tagId.HasValue)
                {
                    var tag = new Tags
                    {
                        Name = tagDto.Name,
                        Category_Id = tagDto.Category_Id,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };

                    await _context.Tags.AddAsync(tag, token);
                    await _context.SaveChangesAsync(token);

                    tagId = tag.ID;

                    if (tagDto.Children != null && tagDto.Children.Count == 0)
                    {
                        var children = tagDto.Children.Select(child => new Tags
                        {
                            Name = child.Name,
                            Parent_Id = tag.ID,
                            Category_Id = tagDto.Category_Id,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        }).ToList();
                        await _context.AddRangeAsync(children, token);
                        //await _context.SaveChangesAsync(token);
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
    }
}
