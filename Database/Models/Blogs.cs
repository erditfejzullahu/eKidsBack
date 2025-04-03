using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Blogs : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public string Title { get; set; }
        public int CategoryId { get; set; }
        public int TagId { get; set; }
        public int UserId { get; set; }
        public int Likes { get; set; }
        public string Content { get; set; }
        public BlogStatus Status { get; set; }
        public string? ImageUrls { get; set; }
        public string? GeneratedContent { get; set; }

        [ForeignKey("CategoryId")]
        public Categories Category { get; set; }

        [ForeignKey("TagId")]
        public Tags Tag { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        public virtual ICollection<BlogLikes> BlogLikes { get; set; } = new List<BlogLikes>();
        public virtual ICollection<BlogComments> BlogComments { get; set; } = new List<BlogComments>();

        public virtual ICollection<Conversations> BlogConversations { get; set; } = new List<Conversations>();
    }
}
