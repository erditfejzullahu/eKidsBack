using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class LessonNavigationResponse
    {
        public int? NextLessonId { get; set; }
        public int? PreviousLessonId { get; set; }
        public int CurrentLessonId { get; set; }
        public bool HasNextLesson { get; set; }
        public bool HasPreviousLesson { get; set; }
    }
}
