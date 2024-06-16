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

        [Required(ErrorMessage = "LessonCategory is required")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "LessonType is required")]
        public string LessonType { get; set; }

        [Required(ErrorMessage = "LessonContent is required")]
        public string LessonContent { get; set; }

        [Required(ErrorMessage = "LessonQuestions is required")]
        public string LessonQuestions { get; set; }

        [Required(ErrorMessage = "LessonAnswers is required")]
        public string LessonAnswers { get; set; }

        [Required(ErrorMessage = "CorrectAnswers is required")]
        public string CorrectAnswers { get; set; }
    }
}
