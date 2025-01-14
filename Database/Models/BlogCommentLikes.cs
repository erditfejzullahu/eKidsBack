using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class BlogCommentLikes : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public BlogComments BlogComment { get; set; }
        public Users User { get; set; }
    }
}
