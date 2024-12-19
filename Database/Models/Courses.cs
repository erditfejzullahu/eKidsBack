using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Courses : BaseModel
    {
        [Key]
        [Column("CourseID")]
        [JsonPropertyName("CourseID")]
        public override int ID { get; set; }

        [Required]
        public string CourseName { get; set; }

        [Required]
        public string CourseDescription { get; set; }

        [Required]
        public int CourseCategory { get; set; }

        [Required]
        public string CourseFeaturedImage { get; set; }

        public int? CourseEnrolled { get; set; }

        public int? ViewCount { get; set; }

        [ForeignKey("CourseCategory")]
        public virtual Categories Category { get; set; }

        public virtual ICollection<Lessons> Lessons { get; set; } = new List<Lessons>();
        public virtual ICollection<CourseCompleted> CourseCompleted { get; set; } = new List<CourseCompleted>();

        //public virtual ICollection<Bookmarks> Bookmarks { get; set; } = new List<Bookmarks>();

        

    }
}
