using Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface ILessonLikesService
    {
        Task<LessonLikes> GetLessonLikeByUser(int lessonId, int userId, CancellationToken token);
        Task AddUserLessonLikeAsync(int lessonId, int userId, CancellationToken token);
        Task RemoveUserLessonLikeAsync(LessonLikes lessonLike, CancellationToken token);
    }
}
