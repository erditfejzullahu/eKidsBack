using Database.Context;
using Database.DTOs;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentService> _logger;

        public CommentService(ApplicationDbContext context, ILogger<CommentService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<List<CommentDto>> GetCommentsAsync(int id, string type, int? userId)
        {
            try
            {
                var query = _context.Comments
                    .Include(c => c.Replies)
                    .Include(c => c.User)
                    .Include(c => c.CommentLikes)
                    .AsQueryable();

                if (type == "lesson")
                {
                    query = query.Where(c => c.LessonId == id);
                }
                else if (type == "user")
                {
                    query = query.Where(c => c.UserId == id);
                }
                var comments = await query
                    .Select(c => new CommentDto
                    {
                        CommentId = c.ID,
                        Comment_Content = c.Comment_Content,
                        ParentId = c.ParentId,
                        User = new UserDto
                        {
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            Email = c.User.Email,
                            PictureUrl = c.User.ProfilePictureUrl
                        },
                        IsLiked = c.CommentLikes.Any((cl => cl.UserId == userId)),
                        Likes = c.Likes,
                        createdAt = c.CreatedAt,
                        Replies = new List<CommentDto>()
                    })
                    .OrderBy(c => c.createdAt)
                    .ToListAsync();

                var commentLookup = comments.ToLookup(c => c.ParentId);

                List<CommentDto> BuildHierarky(int? parentId)
                {
                    return commentLookup[parentId]
                        .Select(c => new CommentDto
                        {
                            CommentId = c.CommentId,
                            Comment_Content = c.Comment_Content,
                            ParentId = c.ParentId,
                            User = new UserDto
                            {
                                Name = c.User.Name,
                                Email = c.User.Email,
                                PictureUrl = c.User.PictureUrl
                            },
                            Likes = c.Likes,
                            IsLiked = c.IsLiked,
                            createdAt = c.createdAt,
                            Replies = BuildHierarky(c.CommentId)
                        })
                        .OrderBy(c => c.createdAt)
                        .ToList();
                }

                return BuildHierarky(null);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retrieving all data");
                throw new ArgumentException("An error occurred while fetching comments.", ex);
            }

        }

        public async Task<int> GetAllLessonCommentsCount(int lessonId, CancellationToken token)
        {
            return await _context.Comments.Where(c => c.LessonId == lessonId).CountAsync(token);
        }

       
    }
}
