using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    
    public class CreateBlogDto
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public BlogStatus Status { get; set; }
        [Required]
        public string Content { get; set; }
        public List<string>? Images { get; set; }
        public List<TagsDto> Tags { get; set; }
    }

    public class TagsDto
    {
        [Required]
        public string Name { get; set; }
    }

    public class BlogRetrieveDto
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int CategoryId { get; set; }
        public int UserId { get; set; }
        public bool IsLiked { get; set; }
        public int Likes { get; set; }
        public BlogRetrieveUserDto User { get; set; }
        public List<BlogRetrieveTagDto> Tags { get; set; } = new List<BlogRetrieveTagDto>();
        public int CommentsCount { get; set; }
        public string Content { get; set; }
        public BlogStatus Status { get; set; }
        public string? ImageUrls { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BlogRetrieveTagDto
    {
        public string Name { get; set; }
        public int TagId { get; set; }
    }
    public class BlogRetrieveUserDto
    {
        public string Name { get; set; }
        public string ProfilePicture { get; set; }
    }
}
