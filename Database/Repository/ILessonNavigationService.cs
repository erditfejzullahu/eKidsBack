using Database.DTOs;
using Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface ILessonNavigationService
    {
        Task<LessonNavigationResponse> GetLessonNavigationAsync(int lessonId, CancellationToken token);
        Task<UserProgress?> GetLessonCompletation(int lessonId, int userId, CancellationToken token);
    }
}
