using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateUserProgress
    {
        [Required(ErrorMessage = "Course ID is required")]
        public int CourseId {  get; set; }

        [Required(ErrorMessage = "Lesson ID is required")]
        public int LessonId { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }


    }
}
