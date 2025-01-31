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
        Task<(List<BlogRetrieveDto> blogs, bool hasMore)> AllBlogRetrieve(int userId, PaginationDto paginationDto, CancellationToken token);
        Task<int> HandleStatusBlogLike(int blogId, int userId, CancellationToken token);
        Task<(List<BlogRetrieveDto> blogs, bool hasMore)> AllBlogByTagRetrieve(int userId, int tagId, PaginationDto paginationDto, CancellationToken token);
        Task<BlogRetrieveDto> GetBlogById(int blogId, int userId, CancellationToken token);
    }
}
