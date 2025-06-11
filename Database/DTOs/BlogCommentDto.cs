using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateBlogComment
    {
        [Required]
        public int BlogId { get; set; }
        [Required]
        public string Comment_Content { get; set; }
        public string? base64Data { get; set; }
        [Required]
        public int UserId { get; set; }
        public int? ParentId { get; set; }

    }

    public class BlogCommentDto
    {
        public int CommentId { get; set; }
        public int BlogId { get; set; }
        public string Comment_Content { get; set; }
        public string? Item_Url { get; set; }
        public int UserId { get; set; }
        public int? ParentId { get; set; }
        public int Likes { get; set; }
        public bool IsLiked { get; set; }
        public BlogUserDto User { get; set; }
        public List<BlogCommentDto> Replies { get; set; } = new List<BlogCommentDto>();
        public DateTime createdAt { get; set; }

    }

    public class BlogUserDto
    {
        public string Name { get; set; }
        public string ProfilePicture { get; set; }
    }
}
