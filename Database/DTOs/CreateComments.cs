using Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateComments
    {
        public int LessonId { get; set; }

        public int UserId { get; set; }

        public int? ParentId { get; set; }

        public string Comment_Content { get; set; }

    }

    public class CommentDto
    {
        public int CommentId { get; set; }
        public string Comment_Content { get; set; }
        public int? ParentId { get; set; }
        public UserDto User { get; set; }
        public int Likes { get; set; }
        public bool IsLiked { get; set; }
        public List<CommentDto> Replies { get; set; } = new List<CommentDto>();
        public DateTime createdAt { get; set; }
    }

    public class UserDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PictureUrl { get; set; }
    }

}
