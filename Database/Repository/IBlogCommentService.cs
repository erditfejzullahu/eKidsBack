using Database.DTOs;
using Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface IBlogCommentService
    {
        Task<BlogComments> CreateBlogComment(CreateBlogComment blogDto, CancellationToken token);
        Task<List<BlogCommentDto>> RetrieveBlogComments(int blogId, int userId, CancellationToken token);
        Task<int> HandleStatusBlogComment(int blogCommentId, int userId, int blogId, CancellationToken token);


    }
}
