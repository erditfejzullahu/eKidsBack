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
    public class CourseCompletationService : ICourseCompletationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseCompletationService> _logger;
        public CourseCompletationService(ApplicationDbContext context, ILogger<CourseCompletationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsCompleted(int courseId, int userId, CancellationToken token)
        {
            try
            {
                var checkCourse = await _context.CourseCompleted.AsNoTracking().AnyAsync(c => c.CourseId == courseId && c.UserId == userId);
                return checkCourse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in checking existance of course with id: {courseId} and userId: {userId}");
                throw new ApplicationException("Error in checking for course completation existance", ex);
            }
        }

        public async Task<CourseCompletationResponse> CompleteCourse(int courseId, int userId, CancellationToken token)
        {
            try
            {
                var isCompleted = await IsCompleted(courseId, userId, token);
                if (isCompleted)
                {
                    return new CourseCompletationResponse
                    {
                        IsCompleted = true,
                        Message = "Course is already completed!"
                    };
                }
                var newCourseCompleted = new CourseCompleted
                {
                    CourseId = courseId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.CourseCompleted.AddAsync(newCourseCompleted, token);
                await _context.SaveChangesAsync(token);

                return new CourseCompletationResponse
                {
                    IsCompleted = true,
                    Message = "Course is completed!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in compeltecourse");
                throw new ApplicationException("Error in generating response for completed course or not", ex);
            }
        }
    }
}
