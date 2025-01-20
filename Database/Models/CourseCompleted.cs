using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class CourseCompleted : BaseModel
    {
        [Column("Id")]
        [Required]
        public override int ID { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int UserId { get; set; }
        public string? Testimonial { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        [ForeignKey("CourseId")]
        public Courses Course { get; set; }
    }
}
