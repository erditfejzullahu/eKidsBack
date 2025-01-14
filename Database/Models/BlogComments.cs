using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class BlogComments : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int BlogId { get; set; }
        public int Likes { get; set; }
        public string Comment_Content { get; set; }
        public string? Item_Url { get; set; }
        public int? ParentId { get; set; }
        public int UserId { get; set; }

        public BlogComments Parent { get; set; }

        [ForeignKey("BlogId")]
        public Blogs Blog { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }
        public ICollection<BlogComments> Replies { get; set; }
        public ICollection<BlogCommentLikes> BlogCommentLikes { get; set; }

    }
}
