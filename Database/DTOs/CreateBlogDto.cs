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
        public string Content { get; set; }

    }

    public class CreateTag
    {
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public int? Category_Id { get; set; }
        public List<CreateTag> Children { get; set; } = new List<CreateTag>();
    }
}
