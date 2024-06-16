using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class UpdateLessons
    {
        public string? LessonName { get; set; }
        public int CategoryID { get; set; }
        public string?[] LessonType { get; set; }
        public string? LessonContent { get; set; }
        public string?[] LessonQuestions { get; set; }
        public string?[] LessonAnswers { get; set; }
        public string?[] CorrectAnswers { get; set; }

    }
}
