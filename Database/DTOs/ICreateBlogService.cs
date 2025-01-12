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
    }
}
