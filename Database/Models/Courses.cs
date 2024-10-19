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

    }
}
