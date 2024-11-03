using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateLessons
    {
        [Required(ErrorMessage = "LessonName is required")]
        public string LessonName { get; set; }

        [Required(ErrorMessage = "LessonContent is required")]
        public string LessonContent { get; set; }

        public string? LessonType { get; set; }

        public string? LessonQuestions { get; set; }

        public string? LessonAnswers { get; set; }

        public string? CorrectAnswers { get; set; }

        [Required(ErrorMessage = "Lesson Image is required")]
        public string LessonFeaturedImage { get; set; }

        [Required(ErrorMessage = "Lesson Excerpt is required")]
        public string LessonExcerpt { get; set; }

        [Required(ErrorMessage = "Course ID is required!")]
        public int CourseID { get; set; }

        [Required(ErrorMessage = "Has Quiz value is required")]
        public bool HasQuiz { get; set; }

        public string? LessonVideo { get; set; }
    }
}
