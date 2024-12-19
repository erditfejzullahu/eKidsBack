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
    public class Bookmarks : BaseModel
    {
        [Required]
        [Column("id")]
        [JsonPropertyName("id")]
        public override int ID { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? CourseId { get; set; }

        [ForeignKey("CourseId")]
        public virtual Courses? Course { get; set; }


        public int? LessonId { get; set; }

        [ForeignKey("LessonId")]
        public virtual Lessons? Lesson { get; set; }
        

    }
}
