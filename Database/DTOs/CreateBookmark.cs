using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateBookmark
    {
        [Required(ErrorMessage = "User id is required")]
        public int UserId { get; set; }

        public int? CourseId { get; set; }

        public int? LessonId { get; set; }

    }
}
