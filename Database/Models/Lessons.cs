using Microsoft.EntityFrameworkCore;
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
    public class Lessons : BaseModel
    {
        [Required]
        [Column("LessonID")]
        [JsonPropertyName("LessonID")]
        public override int ID { get; set; }

        [Required]
        public string LessonName { get; set; }

        [Required]
        public string LessonContent { get; set; }

        public string? LessonType { get; set; }

        public string? LessonQuestions { get; set; }

        public string? LessonAnswers { get; set; }

        public string? CorrectAnswers { get; set; }

        public int? LessonStarted { get; set; }
        public int Likes { get; set; }

        [Required]
        public string LessonFeaturedImage { get; set; }

        [Required]
        public string LessonExcerpt { get; set; }

        [Required]
        public int CourseID { get; set; }

        public string? LessonVideo { get; set; }

        public int? ViewCount { get; set; }

        [ForeignKey("CourseID")]
        public virtual Courses ?Course { get; set; }

        public virtual ICollection<LessonLikes> LessonLikes { get; set; } = new List<LessonLikes>();

        //[Required]
        //public DateTime CreatedAt { get; set; }

        //[Required]
        //public DateTime ModifiedAt { get; set;}

    }
}
