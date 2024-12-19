using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class CommentLikes : BaseModel
    {
        [Required]
        [Column("Id")]
        public override int ID { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CommentID  { get; set; }

        [ForeignKey("UserId")]
        public virtual Users Users { get; set; }

        [ForeignKey("CommentID")]
        public virtual Comments Comments { get; set; }
    }
}
