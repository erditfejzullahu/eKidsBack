using Database.Context;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class CommentLikesService : ICommentLikesService
    {
        private readonly ApplicationDbContext _context;

        public CommentLikesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CommentLikes?> GetUserCommentLikeAsync(int commentId, int userId, CancellationToken token)
        {
            return await _context.CommentLikes.FirstOrDefaultAsync(c => c.CommentID == commentId && c.UserId == userId);
        }

        public async Task AddUserLikeAsync(int commentId, int userId, CancellationToken token)
        {
            var userLike = new CommentLikes
            {
                CommentID = commentId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _context.CommentLikes.Add(userLike);
            await _context.SaveChangesAsync(token);
        }

        public async Task RemoveUserLikeAsync(CommentLikes like, CancellationToken token)
        {
            _context.CommentLikes.Remove(like);
            await _context.SaveChangesAsync(token);
        }


    }
}
