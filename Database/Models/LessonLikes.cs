using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class LessonLikes : BaseModel
    {
        [Required]
        [Column("Id")]
        public override int ID { get; set; }
        public int LessonId { get; set; }
        public int UserId  { get; set; }

        [ForeignKey("UserId")]
        public Users Users { get; set; }

        [ForeignKey("LessonId")]
        public Lessons Lessons { get; set; }


    }
}
