using Database.DTOs;
using Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface ICommentService
    {
        Task<List<CommentDto>> GetCommentsAsync(int id, string type, int? userId);
        Task<int> GetAllLessonCommentsCount(int lessonId, CancellationToken token);
       // Task<List<CreateComments>> GetCommentLevelAsync(int id);
    }
}
