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
    public class LessonLikesService : ILessonLikesService
    {
        private readonly ApplicationDbContext _context;

        public LessonLikesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LessonLikes?> GetLessonLikeByUser(int lessonId, int userId, CancellationToken token)
        {
            return await _context.LessonLikes.FirstOrDefaultAsync(c => c.LessonId == lessonId && c.UserId == userId);
        } 

        public async Task AddUserLessonLikeAsync(int lessonId, int userId, CancellationToken token)
        {
            var userLike = new LessonLikes
            {
                LessonId = lessonId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _context.LessonLikes.Add(userLike);
            await _context.SaveChangesAsync(token);
        }

        public async Task RemoveUserLessonLikeAsync(LessonLikes lessonLike, CancellationToken token)
        {
            _context.LessonLikes.Remove(lessonLike);
            await _context.SaveChangesAsync(token);
        }
    }
}
