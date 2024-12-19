using Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface ICommentLikesService
    {
        Task<CommentLikes> GetUserCommentLikeAsync(int commentId, int userId, CancellationToken token);
        Task AddUserLikeAsync(int commentId, int userId, CancellationToken token);
        Task RemoveUserLikeAsync(CommentLikes like, CancellationToken token);

    }
}
