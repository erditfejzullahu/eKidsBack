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
    public class LessonNavigationService : ILessonNavigationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LessonNavigationService> _logger;

        public LessonNavigationService( ApplicationDbContext context, ILogger<LessonNavigationService> logger )
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<LessonNavigationResponse> GetLessonNavigationAsync(int lessonId, CancellationToken token)
        {
            try
            {
                var groupedLessons = await _context.Lessons.AsNoTracking().OrderBy(i => i.ID).GroupBy(c => c.CourseID).ToListAsync(token);

                var currentLessonGroup = groupedLessons.FirstOrDefault(c => c.Any(l => l.ID == lessonId));
                if (currentLessonGroup == null)
                {
                    throw new KeyNotFoundException($"Lesson with ID {lessonId} not found in any course.");
                }

                var lessons = currentLessonGroup.ToList();
                var currentIndex = lessons.FindIndex(l => l.ID == lessonId);

                var hasPreviousLessons = currentIndex > 0;
                var hasNextLessons = currentIndex < lessons.Count - 1;

                var nextLessonId = hasNextLessons ? lessons[currentIndex + 1].ID : (int?)null;
                var previousLessonId = hasPreviousLessons ? lessons[currentIndex - 1].ID : (int?)null;

                var lessonNavResponse = new LessonNavigationResponse
                {
                    CurrentLessonId = lessonId,
                    HasNextLesson = hasNextLessons,
                    HasPreviousLesson = hasPreviousLessons,
                    NextLessonId = nextLessonId,
                    PreviousLessonId = previousLessonId
                };

                return lessonNavResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retrieving lesson navigation");
                throw new ApplicationException($"An error occurred while retrieving navigation for lesson ID {lessonId}.", ex);
            }

        }

        public async Task<UserProgress?> GetLessonCompletation(int lessonId, int userId, CancellationToken token)
        {
            try
            {
                var getStatus = await _context.UserProgress.AsNoTracking().Where(c => c.LessonId == lessonId && c.UserId == userId).FirstOrDefaultAsync(token);
            
                return getStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retrieving lesson completation progress");
                throw new ApplicationException("Error retrieving lesson copletation progress", ex);
            }
        }
    }
}
