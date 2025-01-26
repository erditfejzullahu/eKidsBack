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
    public class UserProgress : BaseModel
    {
        [Required]
        [Column("id")]
        [JsonPropertyName("id")]
        public override int ID { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CourseId { get; set; }


        [Required]
        public int LessonId  { get; set; }

        [Required]
        public bool IsCompleted { get; set; }

        [Required]
        public bool HasStarted { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        public Lessons Lessons { get; set; }
        public Courses Courses { get; set; }

    }
}
