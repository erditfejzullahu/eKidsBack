using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateBlogDto
    {
        public CreateBlog blogDto;
        public CreateTag? tagDto;
    }

    public class CreateBlog
    {
        public string Title { get; set; }
        public int CategoryId { get; set; }
        public int UserId { get; set; }
        public int? TagId { get; set; }
        public BlogStatus Status { get; set; }
        public string Content { get; set; }

    }

    public class CreateTag
    {
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public int? Category_Id { get; set; }
        public List<CreateTag> Children { get; set; } = new List<CreateTag>();
    }

    public class BlogRetrieveDto
    {
        public string Title { get; set; }
        public int CategoryId { get; set; }
        public int UserId { get; set; }
        public bool IsLiked { get; set; }
        public BlogRetrieveUserDto User { get; set; }
        public BlogRetrieveTagDto Tags { get; set; }
        public int TagId { get; set; }
        public string Content { get; set; }
        public BlogStatus Status { get; set; }
        public string ImageUrls { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BlogRetrieveTagDto
    {
        public string Name { get; set; }
        public int TagId { get; set; }
        public List<BlogRetrieveTagDto> Children = new List<BlogRetrieveTagDto>();
    }
    public class BlogRetrieveUserDto
    {
        public string Name { get; set; }
        public string ProfilePicture { get; set; }
    }
}
