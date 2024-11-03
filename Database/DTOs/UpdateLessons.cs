using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class UpdateLessons
    {
        public string? LessonName { get; set; }

        public string? LessonContent { get; set; }

        public string? LessonType { get; set; }

        public string? LessonQuestions { get; set; }

        public string? CorrectAnswers { get; set; }

        public string? LessonFeaturedImage { get; set; }

        public string? LessonExcerpt { get; set; }

        public int? CourseID { get; set; }

        public string? LessonVideo { get; set; }

    }
}
