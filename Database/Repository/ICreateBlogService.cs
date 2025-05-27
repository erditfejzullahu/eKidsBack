using Database.DTOs;
using Database.Models;
using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface ICreateBlogService
    {
        Task<Blogs> CreateBlog(CreateBlogDto request, CancellationToken token);
        Task<(List<BlogRetrieveDto> blogs, bool hasMore)> AllBlogRetrieve(int userId, PaginationDto paginationDto, CancellationToken token, BlogDiscussionRetrivalType retrivalType, GetFriendBlogsOrAll getFriendBlogsOrAll);
        Task<int> HandleStatusBlogLike(int blogId, int userId, CancellationToken token);
        Task<(List<BlogRetrieveDto> blogs, bool hasMore)> AllBlogByTagRetrieve(int userId, int tagId, PaginationDto paginationDto, CancellationToken token, GetFriendBlogsOrAll getFriendBlogsOrAll);
        Task<BlogRetrieveDto> GetBlogById(int blogId, int userId, CancellationToken token);
    }
}
