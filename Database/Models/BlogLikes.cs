using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class BlogLikes : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int BlogId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("BlogId")]
        public Blogs Blog { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }
    }
}
