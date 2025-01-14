using Database.Models;
using Database.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public interface ICreateBlogService
    {
        Task<Blogs> CreateBlog(CreateBlogDto request, CancellationToken token);
        Task<List<BlogRetrieveDto>> AllBlogRetrieve(int userId, PaginationDto paginationDto, CancellationToken token);
        Task<int> HandleStatusBlogLike(int blogId, int userId, CancellationToken token);
    }
}
