using Database.Context;
using Database.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(ApplicationDbContext context, ILogger<StatisticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int[]> GetStatisticsBasedOfType(StatisticsType type, int year, int userId)
        {
            try
            {
                var monthlyCounts = new int[12];
                switch (type)
                {
                    case StatisticsType.CoursesCompleted:
                        var coursesCompleted = await _context.UserProgress
                            .Where(c => c.UserId == userId && c.CreatedAt.Year == year && c.IsCompleted)
                            .GroupBy(c => c.CreatedAt.Month)
                            .OrderBy(g => g.Key)
                            .Select(g => new { Month = g.Key, Count = g.Count() })
                            .ToListAsync();

                        Array.Clear(monthlyCounts, 0, monthlyCounts.Length);
                        foreach (var item in coursesCompleted)
                        {
                            monthlyCounts[item.Month - 1] = item.Count;
                        }
                        return monthlyCounts;
                    case StatisticsType.OfflineCoursesCreated:
                        throw new NotImplementedException();
                    case StatisticsType.QuizzesCompleted:
                        var quizzesCompleted = await _context.QuizzesCompleted
                            .Where(c => c.UserId == userId && c.CreatedAt.Year == year && c.Completed)
                            .GroupBy(c => c.CreatedAt.Month)
                            .OrderBy(g => g.Key)
                            .Select(g => new { Month = g.Key, Count = g.Count() })
                            .ToListAsync();
                        Array.Clear(monthlyCounts, 0, monthlyCounts.Length);
                        foreach (var item in quizzesCompleted)
                        {
                            monthlyCounts[item.Month - 1] = item.Count;
                        }
                        return monthlyCounts;
                    case StatisticsType.QuizzesCreated:
                        var quizzesCreated = await _context.Quizzes
                            .Where(c => c.UserId == userId && c.CreatedAt.Year == year)
                            .GroupBy(c => c.CreatedAt.Month)
                            .OrderBy(g => g.Key)
                            .Select(g => new { Month = g.Key, Count = g.Count() })
                            .ToListAsync();
                        Array.Clear(monthlyCounts, 0, monthlyCounts.Length);
                        foreach (var item in quizzesCreated)
                        {
                            monthlyCounts[item.Month - 1] = item.Count;
                        }
                        return monthlyCounts;
                    case StatisticsType.BlogsCreated:
                        var blogs = await _context.Blogs
                            .Where(c => c.UserId == userId && c.CreatedAt.Year == year)
                            .GroupBy(c => c.CreatedAt.Month)
                            .OrderBy(g => g.Key)
                            .Select(g => new { Month = g.Key, Count = g.Count() })
                            .ToListAsync();
                        Array.Clear(monthlyCounts, 0, monthlyCounts.Length);
                        foreach (var item in blogs)
                        {
                            monthlyCounts[item.Month - 1] = item.Count;
                        }
                        return monthlyCounts;
                    case StatisticsType.DiscussionsCreated:
                        var discussions = await _context.Discussions
                            .Where(c => c.UserId == userId && c.CreatedAt.Year == year)
                            .GroupBy(c => c.CreatedAt.Month)
                            .OrderBy(g => g.Key)
                            .Select(g => new { Month = g.Key, Count = g.Count() })
                            .ToListAsync();
                        Array.Clear(monthlyCounts, 0, monthlyCounts.Length);
                        foreach (var item in discussions)
                        {
                            monthlyCounts[item.Month - 1] = item.Count;
                        }
                        return monthlyCounts;
                    case StatisticsType.OnlineMeetingsAttended:
                        var onlineMeetingsAttended = await _context.StudentCourseLessonProgress
                            .Where(c => c.UserId == userId && c.IsCompleted && c.CreatedAt.Year == year)
                            .GroupBy(c => c.CreatedAt.Month)
                            .OrderBy(g => g.Key)
                            .Select(g => new { Month = g.Key, Count = g.Count() })
                            .ToListAsync();
                        Array.Clear(monthlyCounts, 0, monthlyCounts.Length);
                        foreach (var item in onlineMeetingsAttended)
                        {
                            monthlyCounts[item.Month - 1] = item.Count;
                        }
                        return monthlyCounts;
                    case StatisticsType.CommitmentsMade:
                        var commits = await _context.Commits
                            .Where(c => userId == userId && c.CreatedAt.Year == year)
                            .GroupBy(c => c.CreatedAt.Month)
                            .OrderBy(g => g.Key)
                            .Select(g => new { Month = g.Key, Count = g.Count() })
                            .ToListAsync();
                        Array.Clear(monthlyCounts, 0, monthlyCounts.Length);
                        foreach (var item in commits)
                        {
                            monthlyCounts[item.Month - 1] = item.Count;
                        }
                        return monthlyCounts;
                    default:
                        throw new ApplicationException("Type not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting statistics with userid:{userId}, year:{year}, type:{type}");
                throw new ApplicationException("Error getting statistics");
            }
        }
    }
}
