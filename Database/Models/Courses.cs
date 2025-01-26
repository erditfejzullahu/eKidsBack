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
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        [ForeignKey("CourseCategory")]
        public virtual Categories Category { get; set; }

        public virtual ICollection<Lessons> Lessons { get; set; } = new List<Lessons>();
        public virtual ICollection<CourseCompleted> CourseCompleted { get; set; } = new List<CourseCompleted>();
        public virtual ICollection<UserProgress> CoursesProgress { get; set; } = new List<UserProgress>();

        //public virtual ICollection<Bookmarks> Bookmarks { get; set; } = new List<Bookmarks>();

        

    }
}
